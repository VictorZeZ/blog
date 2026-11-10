using blog.Api.Common;
using blog.Domain.Exceptions;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Diagnostics;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace blog.Api.Middlewares
{
    public sealed partial class RequestLoggingMiddleware(RequestDelegate next, ILoggerFactory loggerFactory, IWebHostEnvironment env)
    {
        private const int MaxBodyChars = 64 * 1024;
        private const int MaxStringLength = 120;
        private const int MaxArrayItems = 3;
        private const int MaxLineLength = 800;
        private const int MaxValueLength = 200;
        private const int LabelWidth = 9;
        private const string Indent = "   ";
        private const string Redacted = "[REDACTED]";

        private static readonly string ContinuationIndent = new(' ', Indent.Length + LabelWidth + 2);

        private static readonly JsonSerializerOptions BodyJsonOptions = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        private static readonly JsonSerializerOptions DetailsJsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private static readonly HashSet<string> SensitiveFields =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "password", "currentPassword", "newPassword", "passwordHash",
                "code", "token", "refreshToken", "accessToken", "secret",
                "publicKey", "privateKey", "cardNumber", "cvv", "ssn", "nationalId"
            };

        private static long _requestCounter;

        private readonly ILogger _logger = loggerFactory.CreateLogger("HTTP");

        public async Task InvokeAsync(HttpContext context)
        {
            if (IsDocumentationRequest(context.Request.Path))
            {
                await next(context);
                return;
            }

            var captureBodies = env.IsDevelopment() && _logger.IsEnabled(LogLevel.Debug);
            var stopwatch = Stopwatch.StartNew();

            var requestBody = captureBodies ? await ReadRequestBodyAsync(context.Request) : null;

            var originalBody = context.Response.Body;
            var capture = captureBodies ? new MemoryStream() : null;
            if (capture is not null)
                context.Response.Body = capture;

            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                context.Items.TryAdd(HttpContextItemKeys.HandledException, ex);
                throw;
            }
            finally
            {
                stopwatch.Stop();

                var responseBody = capture is null
                    ? null
                    : await RestoreResponseBodyAsync(context, originalBody, capture);

                Log(context, stopwatch.ElapsedMilliseconds, requestBody, responseBody);
            }
        }

        private void Log(HttpContext context, long elapsedMs, string? requestBody, string? responseBody)
        {
            var exception = context.Items.TryGetValue(HttpContextItemKeys.HandledException, out var item)
                ? item as Exception
                : null;

            var statusCode = exception is not null && context.Response.StatusCode < 400
                ? StatusCodes.Status500InternalServerError
                : context.Response.StatusCode;

            var level = ResolveLevel(statusCode, exception);
            var debug = _logger.IsEnabled(LogLevel.Debug);

            if (!_logger.IsEnabled(level) || IsSuccessfulPreflight(context.Request, statusCode, debug))
                return;

            var development = env.IsDevelopment();
            var trace = _logger.IsEnabled(LogLevel.Trace);

            var block = new StringBuilder();
            AppendHeaderLine(block, context, statusCode, exception, elapsedMs);

            if (development && debug)
                AppendField(block, "caller", DescribeCaller(context));

            if (development && trace)
            {
                AppendField(block, "headers", DescribeHeaders(context.Request.Headers));
                AppendField(block, "resp-hdrs", DescribeHeaders(context.Response.Headers));
            }

            if (development && debug)
            {
                if (requestBody is not null)
                    AppendField(block, "request", requestBody);

                if (responseBody is not null)
                    AppendField(block, "response", responseBody);
            }

            if (exception is not null)
                AppendException(block, exception, development, debug);

            _logger.Log(level, "{HttpRequest}", block.ToString());
        }

        // ──────────────────────────────────────────────────────────────
        //  Block building
        // ──────────────────────────────────────────────────────────────
        private static void AppendHeaderLine(StringBuilder block, HttpContext context, int statusCode, Exception? exception, long elapsedMs)
        {
            var code = exception switch
            {
                DomainException domain => domain.ErrorCode,
                not null => "UNKNOWN_ERROR",
                _ => null
            };

            block.Append('#').Append(Interlocked.Increment(ref _requestCounter))
                .Append("  ").Append(statusCode);

            if (code is not null)
                block.Append(' ').Append(code);

            block.Append("  ").Append(context.Request.Method)
                .Append("  ").Append(context.Request.Path).Append(context.Request.QueryString.Value)
                .Append("  ").Append(elapsedMs).Append(" ms")
                .Append("  id=").Append(context.TraceIdentifier);

            if (statusCode == CustomStatusCodes.ClientClosedRequest)
                block.Append("  (client closed request)");
        }

        private static void AppendField(StringBuilder block, string label, string value)
            => block.AppendLine().Append(Indent).Append(label.PadRight(LabelWidth)).Append("  ").Append(value);

        private static void AppendContinuation(StringBuilder block, string text)
            => block.AppendLine().Append(ContinuationIndent).Append(text);

        private static void AppendMembers(StringBuilder block, IReadOnlyList<(string Key, string Value)> members)
        {
            if (members.Count == 0)
                return;

            var width = members.Max(m => m.Key.Length) + 3;
            foreach (var (key, value) in members)
                AppendContinuation(block, key.PadRight(width) + Clean(value, MaxValueLength));
        }

        private static void AppendException(StringBuilder block, Exception exception, bool development, bool debug)
        {
            AppendField(block, "exception", exception.GetType().Name);
            AppendMembers(block, BuildExceptionMembers(exception, development));

            if (!debug)
                return;

            AppendStack(block, exception);

            for (var inner = exception.InnerException; inner is not null; inner = inner.InnerException)
            {
                var members = new List<(string Key, string Value)> { ("message", FirstLine(inner.Message)) };
                if (FindOrigin(inner) is { } origin)
                    members.Add(("thrown", origin));

                AppendField(block, "caused by", inner.GetType().Name);
                AppendMembers(block, members);
            }
        }

        private static List<(string Key, string Value)> BuildExceptionMembers(Exception exception, bool development)
        {
            var members = new List<(string Key, string Value)>();

            if (exception is DomainException domain)
            {
                members.Add(("code", domain.ErrorCode));
                members.Add(("status", domain.StatusCode.ToString(CultureInfo.InvariantCulture)));

                if (development)
                {
                    members.Add(("title", domain.Title));
                    members.AddRange(FlattenDetails(domain.Details));
                }
            }
            else if (development)
            {
                members.Add(("message", FirstLine(exception.Message)));
            }

            if ((development || exception is not DomainException) && FindOrigin(exception) is { } origin)
                members.Add(("thrown", origin));

            return members;
        }

        private static IEnumerable<(string Key, string Value)> FlattenDetails(object? details)
        {
            if (details is null)
                yield break;

            var element = JsonSerializer.SerializeToElement(details, DetailsJsonOptions);
            if (element.ValueKind != JsonValueKind.Object)
                yield break;

            foreach (var property in element.EnumerateObject())
            {
                if (property.Name == "errors" && property.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var error in property.Value.EnumerateArray())
                        yield return (ReadString(error, "field"), ReadString(error, "reason"));

                    continue;
                }

                yield return (property.Name, DescribeJson(property.Value));
            }
        }

        private static string ReadString(JsonElement element, string name)
            => element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value)
                ? DescribeJson(value)
                : string.Empty;

        private static string DescribeJson(JsonElement element) => element.ValueKind switch
        {
            JsonValueKind.String => FormatTimestamp(element.GetString() ?? string.Empty),
            JsonValueKind.Array => string.Join(", ", element.EnumerateArray().Select(DescribeJson)),
            JsonValueKind.Null => "null",
            _ => element.GetRawText()
        };

        private static string FormatTimestamp(string value)
            => value.Length >= 19 && value[4] == '-' && value[10] == 'T'
               && DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var timestamp)
                ? timestamp.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss'Z'", CultureInfo.InvariantCulture)
                : value;

        // ──────────────────────────────────────────────────────────────
        //  Stack traces
        // ──────────────────────────────────────────────────────────────
        [GeneratedRegex(@"^at\s+(?<method>blog\.[^\s(]+)\(.*?\)(?: in (?<file>.+?):line (?<line>\d+))?$")]
        private static partial Regex ProjectFrame();

        private static IEnumerable<string> StackLines(Exception exception)
            => (exception.StackTrace ?? string.Empty)
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        private static string? FindOrigin(Exception exception)
        {
            foreach (var line in StackLines(exception))
            {
                var match = ProjectFrame().Match(line);
                if (match.Success)
                    return FormatFrame(match);
            }

            return null;
        }

        private static string FormatFrame(Match match)
        {
            var parts = match.Groups["method"].Value.Replace("..", ".").Split('.');
            var method = parts[^1];
            var type = parts.Length > 1 ? parts[^2].Split('`')[0] : string.Empty;

            var location = match.Groups["file"].Success
                ? $" ({Path.GetFileName(match.Groups["file"].Value)}:{match.Groups["line"].Value})"
                : string.Empty;

            return $"{type}.{method}{location}";
        }

        private static void AppendStack(StringBuilder block, Exception exception)
        {
            var lines = new List<string>();
            var skipped = 0;

            foreach (var line in StackLines(exception))
            {
                var match = ProjectFrame().Match(line);
                if (!match.Success)
                {
                    if (line.StartsWith("at ", StringComparison.Ordinal))
                        skipped++;

                    continue;
                }

                FlushSkippedFrames(lines, ref skipped);
                lines.Add($"at {FormatFrame(match)}");
            }

            FlushSkippedFrames(lines, ref skipped);

            if (lines.Count == 0)
                return;

            AppendField(block, "stack", lines[0]);
            foreach (var line in lines.Skip(1))
                AppendContinuation(block, line);
        }

        private static void FlushSkippedFrames(List<string> lines, ref int skipped)
        {
            if (skipped == 0)
                return;

            lines.Add($"… {skipped} framework frames");
            skipped = 0;
        }

        // ──────────────────────────────────────────────────────────────
        //  Level, caller, headers
        // ──────────────────────────────────────────────────────────────
        private static LogLevel ResolveLevel(int statusCode, Exception? exception) => exception switch
        {
            DomainException { ErrorCode: "LOCKED" or "FORBIDDEN" or "UNAUTHORIZED" or "RATE_LIMIT_EXCEEDED" } => LogLevel.Warning,
            DomainException { ErrorCode: "UNAVAILABLE" or "UNKNOWN_ERROR" } => LogLevel.Error,
            DomainException => LogLevel.Information,
            not null => LogLevel.Error,
            _ => statusCode switch
            {
                401 or 403 or 429 => LogLevel.Warning,
                >= 500 => LogLevel.Error,
                _ => LogLevel.Information
            }
        };

        private static string DescribeCaller(HttpContext context)
        {
            var address = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userId = context.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (userId is null)
                return $"anonymous · {address}";

            var role = context.User.FindFirstValue(ClaimTypes.Role) ?? "unknown";
            var shortId = userId.Length > 12 ? $"{userId[..4]}…{userId[^4..]}" : userId;

            return $"{shortId} · {role} · {address}";
        }

        private static string DescribeHeaders(IHeaderDictionary headers)
            => string.Join(" | ", headers.Select(h => $"{h.Key}: {MaskHeader(h.Key, h.Value.ToString())}"));

        private static string MaskHeader(string name, string value)
        {
            if (name.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
                return value.Split(' ', 2) is [var scheme, _] ? $"{scheme} {Redacted}" : Redacted;

            if (name.Equals("Cookie", StringComparison.OrdinalIgnoreCase) || name.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase))
                return Redacted;

            return value;
        }

        private static bool IsDocumentationRequest(PathString path)
            => path.StartsWithSegments("/scalar") || path.StartsWithSegments("/openapi");

        private static bool IsSuccessfulPreflight(HttpRequest request, int statusCode, bool debug)
            => HttpMethods.IsOptions(request.Method) && statusCode < 400 && !debug;

        // ──────────────────────────────────────────────────────────────
        //  Body capture
        // ──────────────────────────────────────────────────────────────
        private static async Task<string?> ReadRequestBodyAsync(HttpRequest request)
        {
            var contentType = request.ContentType;
            if (request.ContentLength == 0 || string.IsNullOrWhiteSpace(contentType))
                return null;

            if (!IsTextBody(contentType) || request.ContentLength > MaxBodyChars)
                return DescribeBinary(contentType, request.ContentLength);

            request.EnableBuffering();
            var body = await ReadTextAsync(request.Body);
            request.Body.Position = 0;

            return FormatBody(contentType, body);
        }

        private static async Task<string?> RestoreResponseBodyAsync(HttpContext context, Stream originalBody, MemoryStream capture)
        {
            context.Response.Body = originalBody;

            var description = DescribeResponse(context.Response.ContentType, capture);

            try
            {
                capture.Position = 0;
                await capture.CopyToAsync(originalBody);
            }
            catch (Exception ex) when (ex is IOException or OperationCanceledException)
            {
                return description;
            }
            finally
            {
                await capture.DisposeAsync();
            }

            return description;
        }

        private static string? DescribeResponse(string? contentType, MemoryStream capture)
        {
            if (capture.Length == 0 || string.IsNullOrWhiteSpace(contentType))
                return null;

            if (!IsTextBody(contentType) || capture.Length > MaxBodyChars)
                return DescribeBinary(contentType, capture.Length);

            return FormatBody(contentType, Encoding.UTF8.GetString(capture.GetBuffer(), 0, (int)capture.Length));
        }

        private static async Task<string> ReadTextAsync(Stream stream)
        {
            try
            {
                using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
                var buffer = new char[MaxBodyChars];
                var read = await reader.ReadBlockAsync(buffer, 0, buffer.Length);

                return new string(buffer, 0, read);
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        private static bool IsTextBody(string contentType)
            => contentType.Contains("json", StringComparison.OrdinalIgnoreCase)
               || contentType.Contains("x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase)
               || contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase);

        private static string DescribeBinary(string contentType, long? length)
        {
            var mediaType = contentType.Split(';')[0].Trim();
            return length is null ? $"({mediaType})" : $"({mediaType}, {FormatSize(length.Value)})";
        }

        private static string FormatSize(long bytes) => bytes switch
        {
            < 1024 => $"{bytes} B",
            < 1024 * 1024 => $"{(bytes / 1024d).ToString("0.#", CultureInfo.InvariantCulture)} KB",
            _ => $"{(bytes / (1024d * 1024d)).ToString("0.#", CultureInfo.InvariantCulture)} MB"
        };

        private static string? FormatBody(string contentType, string body)
        {
            if (string.IsNullOrWhiteSpace(body))
                return null;

            if (contentType.Contains("json", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using var document = JsonDocument.Parse(body);
                    return Clean(JsonSerializer.Serialize(MaskJson(document.RootElement), BodyJsonOptions), MaxLineLength);
                }
                catch (JsonException)
                {
                    return $"(invalid JSON, {Count(body.Length)} chars)";
                }
            }

            return contentType.Contains("x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase)
                ? Clean(MaskForm(body), MaxLineLength)
                : Clean(body, MaxLineLength);
        }

        private static object? MaskJson(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    var properties = new Dictionary<string, object?>();
                    foreach (var property in element.EnumerateObject())
                        properties[property.Name] = SensitiveFields.Contains(property.Name) ? Redacted : MaskJson(property.Value);
                    return properties;

                case JsonValueKind.Array:
                    var items = element.EnumerateArray().Take(MaxArrayItems).Select(MaskJson).ToList();
                    var total = element.GetArrayLength();
                    if (total > MaxArrayItems)
                        items.Add($"…(+{total - MaxArrayItems} more)");
                    return items;

                case JsonValueKind.String:
                    var text = element.GetString() ?? string.Empty;
                    return text.Length > MaxStringLength ? $"<{Count(text.Length)} chars>" : text;

                case JsonValueKind.Number:
                    return element.TryGetInt64(out var integer) ? integer : (object)element.GetDouble();

                case JsonValueKind.True:
                    return true;

                case JsonValueKind.False:
                    return false;

                default:
                    return null;
            }
        }

        private static string MaskForm(string body)
        {
            var pairs = body.Split('&').Select(pair =>
            {
                var separator = pair.IndexOf('=');
                if (separator < 0)
                    return pair;

                var key = Uri.UnescapeDataString(pair[..separator]);
                return SensitiveFields.Contains(key) ? $"{key}={Redacted}" : pair;
            });

            return string.Join('&', pairs);
        }

        // ──────────────────────────────────────────────────────────────
        //  Text helpers
        // ──────────────────────────────────────────────────────────────
        private static string FirstLine(string message)
            => message.ReplaceLineEndings("\n").Split('\n')[0];

        private static string Clean(string value, int maxLength)
        {
            var text = value.ReplaceLineEndings(" ");
            return text.Length <= maxLength
                ? text
                : $"{text[..maxLength]}… (+{Count(text.Length - maxLength)} chars)";
        }

        private static string Count(int value)
            => value.ToString("N0", CultureInfo.InvariantCulture);
    }
}