using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace blog.Api.Middlewares
{
    public sealed class RequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingMiddleware> logger,
        IWebHostEnvironment env)
    {
        // ──────────────────────────────────────────────────────────────
        //  Constants
        // ──────────────────────────────────────────────────────────────
        private const int MaxBodyBytes = 64 * 1024; // 64 KB

        // ── thread-safe request counter (resets on app restart) ───────
        private static long _requestCounter;

        private static readonly HashSet<string> SensitiveFields =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "password", "passwordHash", "token", "refreshToken",
                "accessToken", "secret", "publicKey", "privateKey",
                "cardNumber", "cvv", "ssn", "nationalId"
            };

        // ──────────────────────────────────────────────────────────────
        //  Pipeline entry point
        // ──────────────────────────────────────────────────────────────
        public async Task InvokeAsync(HttpContext context)
        {
            var sw = Stopwatch.StartNew();
            var requestNumber = Interlocked.Increment(ref _requestCounter);
            var correlationId = context.TraceIdentifier;

            var requestBody = await CaptureRequestBodyAsync(context);
            var (responseBody, originalResponseBody, responseStream) =
                await SetupResponseCaptureAsync(context);

            try
            {
                await next(context);
                sw.Stop();

                responseBody = await ReadCapturedResponseAsync(
                    context, responseStream, responseBody);

                LogRequest(context, sw.ElapsedMilliseconds, correlationId,
                    requestBody, responseBody, requestNumber);
            }
            catch (Exception ex)
            {
                sw.Stop();
                LogException(context, sw.ElapsedMilliseconds, correlationId,
                    requestBody, ex, requestNumber);
                throw;
            }
            finally
            {
                context.Response.Body = originalResponseBody;

                if (responseStream is not null)
                {
                    if (responseStream.CanSeek && responseStream.Position != 0)
                        responseStream.Position = 0;

                    await responseStream.CopyToAsync(originalResponseBody);
                    await responseStream.DisposeAsync();
                }
            }
        }

        // ──────────────────────────────────────────────────────────────
        //  Request capture
        // ──────────────────────────────────────────────────────────────
        private async Task<string> CaptureRequestBodyAsync(HttpContext context)
        {
            if (!env.IsDevelopment())
                return "(skipped in production)";

            if (!IsReadableBody(context.Request.ContentType, context.Request.ContentLength))
                return "(binary or empty)";

            context.Request.EnableBuffering();
            var body = await ReadStreamSafeAsync(context.Request.Body);
            context.Request.Body.Position = 0;
            return body;
        }

        // ──────────────────────────────────────────────────────────────
        //  Response capture setup
        // ──────────────────────────────────────────────────────────────
        private Task<(string responseBody, Stream originalBody, MemoryStream? captureStream)>
            SetupResponseCaptureAsync(HttpContext context)
        {
            var originalBody = context.Response.Body;

            if (!env.IsDevelopment())
                return Task.FromResult<(string, Stream, MemoryStream?)>(
                    ("(skipped in production)", originalBody, null));

            var captureStream = new MemoryStream();
            context.Response.Body = captureStream;

            return Task.FromResult<(string, Stream, MemoryStream?)>(
                ("(pending)", originalBody, captureStream));
        }

        private async Task<string> ReadCapturedResponseAsync(
            HttpContext context, MemoryStream? captureStream, string fallback)
        {
            if (captureStream is null)
                return fallback;

            if (!IsReadableBody(context.Response.ContentType, context.Response.ContentLength))
                return "(binary or empty)";

            captureStream.Position = 0;
            return await ReadStreamSafeAsync(captureStream);
        }

        // ──────────────────────────────────────────────────────────────
        //  Structured log output — the pretty box format
        // ──────────────────────────────────────────────────────────────
        private void LogRequest(
            HttpContext ctx,
            long elapsedMs,
            string correlationId,
            string requestBody,
            string responseBody,
            long requestNumber)
        {
            var method = ctx.Request.Method;
            var path = ctx.Request.Path + ctx.Request.QueryString;
            var statusCode = ctx.Response.StatusCode;

            // ── always-on summary line ────────────────────────────────
            var logLevel = statusCode >= 500 ? LogLevel.Error
                         : statusCode >= 400 ? LogLevel.Warning
                         : LogLevel.Information;

            logger.Log(logLevel,
                "\n┌─ #{RequestNumber}  {Method} {Path}\n│  Status  : {StatusCode}  -  {ElapsedMs}ms\n│  CorrelId: {CorrelationId}\n└────────────────────────────────────────────",
                requestNumber, method, path, statusCode, elapsedMs, correlationId);

            // ── auth failures ─────────────────────────────────────────
            if (statusCode is 401 or 403)
            {
                logger.LogWarning(
                    "!! AUTH FAILURE [{StatusCode}] {Method} {Path}  CorrelationId={CorrelationId}",
                    statusCode, method, path, correlationId);
            }

            // ── detailed debug block (Development only) ───────────────
            if (!logger.IsEnabled(LogLevel.Debug) || !env.IsDevelopment())
                return;

            var reqHeaders = MaskHeaders(ctx.Request.Headers);
            var respHeaders = MaskHeaders(ctx.Response.Headers);
            var reqBody = MaskSensitiveBody(requestBody);
            var respBody = MaskSensitiveBody(responseBody);

            var sb = new StringBuilder();
            sb.AppendLine("╔══════════════════════════════════════════════════════════════════");
            sb.AppendLine($"║  #{requestNumber}  {method,-8} {path}");
            sb.AppendLine("╠══════════════════════════════════════════════════════════════════");

            sb.AppendLine("║  REQUEST HEADERS");
            foreach (var (k, v) in reqHeaders)
                sb.AppendLine($"║    {k,-30} {v}");

            if (!string.IsNullOrWhiteSpace(reqBody))
            {
                sb.AppendLine("║  REQUEST BODY");
                foreach (var line in Indent(reqBody))
                    sb.AppendLine($"║    {line}");
            }

            sb.AppendLine("╠══════════════════════════════════════════════════════════════════");
            sb.AppendLine($"║  RESPONSE  {statusCode}  {StatusEmoji(statusCode)}  →  {elapsedMs}ms");
            sb.AppendLine("║  RESPONSE HEADERS");
            foreach (var (k, v) in respHeaders)
                sb.AppendLine($"║    {k,-30} {v}");

            if (!string.IsNullOrWhiteSpace(respBody))
            {
                sb.AppendLine("║  RESPONSE BODY");
                foreach (var line in Indent(respBody))
                    sb.AppendLine($"║    {line}");
            }

            sb.AppendLine($"║  CorrelationId: {correlationId}");
            sb.AppendLine("╚══════════════════════════════════════════════════════════════════");

            logger.LogDebug("{HttpDetails}", sb.ToString());
        }

        // ──────────────────────────────────────────────────────────────
        //  Exception log
        // ──────────────────────────────────────────────────────────────
        private void LogException(
            HttpContext ctx,
            long elapsedMs,
            string correlationId,
            string requestBody,
            Exception ex,
            long requestNumber)
        {
            var sb = new StringBuilder();
            sb.AppendLine("╔══════════════════════════════════════════════════════════════════");
            sb.AppendLine($"║  #{requestNumber}  *** UNHANDLED EXCEPTION ***");
            sb.AppendLine($"║  {ctx.Request.Method,-8} {ctx.Request.Path}{ctx.Request.QueryString}");
            sb.AppendLine($"║  CorrelationId: {correlationId}  •  {elapsedMs}ms");
            sb.AppendLine("╠══════════════════════════════════════════════════════════════════");
            sb.AppendLine($"║  {ex.GetType().Name}: {ex.Message}");

            if (ex.InnerException is not null)
                sb.AppendLine($"║  Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");

            if (env.IsDevelopment() && ex.StackTrace is not null)
            {
                sb.AppendLine("║  Stack Trace:");
                foreach (var line in ex.StackTrace
                    .Split('\n')
                    .Take(10)
                    .Select(l => l.TrimEnd()))
                    sb.AppendLine($"║    {line}");
            }

            if (env.IsDevelopment())
            {
                var maskedReq = MaskSensitiveBody(requestBody);
                sb.AppendLine("║  Request Body (at time of exception):");
                foreach (var line in Indent(maskedReq))
                    sb.AppendLine($"║    {line}");
            }

            sb.AppendLine("╚══════════════════════════════════════════════════════════════════");

            logger.LogError(ex, "{ExceptionDetails}", sb.ToString());
        }

        // ──────────────────────────────────────────────────────────────
        //  Body reading — fixed: no stream.Length on non-seekable streams
        // ──────────────────────────────────────────────────────────────
        private static async Task<string> ReadStreamSafeAsync(Stream stream)
        {
            try
            {
                if (stream.CanSeek)
                    stream.Position = 0;

                using var ms = new MemoryStream();
                var buffer = new byte[4096];
                int read;
                int total = 0;

                while ((read = await stream.ReadAsync(buffer)) > 0)
                {
                    var toWrite = Math.Min(read, MaxBodyBytes - total);
                    ms.Write(buffer, 0, toWrite);
                    total += toWrite;

                    if (total >= MaxBodyBytes)
                    {
                        var truncated = Encoding.UTF8.GetString(ms.ToArray());
                        return truncated + $"\n...(truncated at {MaxBodyBytes / 1024}KB)";
                    }
                }

                var text = Encoding.UTF8.GetString(ms.ToArray());
                return string.IsNullOrWhiteSpace(text) ? "(empty body)" : text;
            }
            catch (Exception ex)
            {
                return $"(unreadable: {ex.GetType().Name})";
            }
        }

        // ──────────────────────────────────────────────────────────────
        //  Body filter — fixed: null ContentLength no longer skips
        // ──────────────────────────────────────────────────────────────
        private static bool IsReadableBody(string? contentType, long? contentLength)
        {
            // contentLength==null means Transfer-Encoding: chunked — still try to read
            if (contentLength is not null and (0 or > MaxBodyBytes))
                return false;

            if (string.IsNullOrWhiteSpace(contentType))
                return false;

            var ct = contentType.ToLowerInvariant();

            return !ct.Contains("image") &&
                   !ct.Contains("audio") &&
                   !ct.Contains("video") &&
                   !ct.Contains("pdf") &&
                   !ct.Contains("octet-stream") &&
                   !ct.Contains("zip") &&
                   !ct.Contains("binary");
        }

        // ──────────────────────────────────────────────────────────────
        //  Header masking
        // ──────────────────────────────────────────────────────────────
        private static IDictionary<string, string> MaskHeaders(IHeaderDictionary headers)
        {
            var result = new Dictionary<string, string>(
                headers.Count, StringComparer.OrdinalIgnoreCase);

            foreach (var h in headers)
            {
                var val = string.Join(", ", h.Value.ToArray());

                result[h.Key] = h.Key switch
                {
                    var k when k.Equals("Authorization", StringComparison.OrdinalIgnoreCase) =>
                        string.IsNullOrEmpty(val) ? "(empty)"
                        : val.Split(' ', 2) is [var scheme, _]
                            ? $"{scheme} [REDACTED]"
                            : "[REDACTED]",

                    var k when k.Equals("Cookie", StringComparison.OrdinalIgnoreCase) =>
                        "[REDACTED]",

                    var k when k.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase) =>
                        "[REDACTED]",

                    _ => val.Length > 2048 ? val[..2048] + "...(truncated)" : val
                };
            }

            return result;
        }

        // ──────────────────────────────────────────────────────────────
        //  Body masking — fixed: handles non-JSON bodies gracefully
        // ──────────────────────────────────────────────────────────────
        private static string MaskSensitiveBody(string body)
        {
            if (string.IsNullOrWhiteSpace(body) ||
                body.StartsWith('('))           // already a sentinel like "(empty body)"
                return body;

            // Try JSON first
            if (body.TrimStart() is ['{', ..] or ['[', ..])
            {
                try
                {
                    using var doc = JsonDocument.Parse(body);
                    var masked = MaskJsonElement(doc.RootElement);
                    return JsonSerializer.Serialize(masked, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });
                }
                catch
                {
                    // fall through to plain text
                }
            }

            // Form-encoded body: mask sensitive keys
            if (body.Contains('=') && body.Contains('&'))
            {
                var parts = body.Split('&');
                var masked = parts.Select(part =>
                {
                    var eq = part.IndexOf('=');
                    if (eq < 0) return part;
                    var key = Uri.UnescapeDataString(part[..eq]);
                    return SensitiveFields.Contains(key)
                        ? $"{key}=[REDACTED]"
                        : part;
                });
                return string.Join("&", masked);
            }

            // Plain text / XML / other: return as-is (truncated if huge)
            return body.Length > 2000
                ? body[..2000] + "...(display truncated)"
                : body;
        }

        private static object? MaskJsonElement(JsonElement el)
        {
            switch (el.ValueKind)
            {
                case JsonValueKind.Object:
                    var dict = new Dictionary<string, object?>(el.GetArrayLength());
                    foreach (var prop in el.EnumerateObject())
                    {
                        dict[prop.Name] = SensitiveFields.Contains(prop.Name)
                            ? "[REDACTED]"
                            : MaskJsonElement(prop.Value);
                    }
                    return dict;

                case JsonValueKind.Array:
                    return el.EnumerateArray().Select(MaskJsonElement).ToList();

                case JsonValueKind.Number:
                    return el.TryGetInt64(out var l) ? (object)l : el.GetDouble();

                case JsonValueKind.True: return true;
                case JsonValueKind.False: return false;
                case JsonValueKind.Null: return null;

                default:
                    return el.GetString();
            }
        }

        // ──────────────────────────────────────────────────────────────
        //  Helpers
        // ──────────────────────────────────────────────────────────────
        private static IEnumerable<string> Indent(string text) =>
            text.Split('\n').Select(l => l.TrimEnd());

        private static string StatusEmoji(int code) => code switch
        {
            >= 500 => "[ERR]",
            >= 400 => "[WRN]",
            >= 300 => "[RDR]",
            >= 200 => "[OK]",
            _ => "[???]"
        };
    }
}