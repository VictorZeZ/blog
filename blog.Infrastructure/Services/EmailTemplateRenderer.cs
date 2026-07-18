using blog.Domain.Common.Interfaces;

namespace blog.Infrastructure.Services
{
    public class EmailTemplateRenderer : IEmailTemplateRenderer
    {
        public string RenderVerificationCode(string title, string description, string code, int expiryMinutes)
        {
            return $$"""
                <!DOCTYPE html>
                <html>
                <head>
                  <meta charset="utf-8">
                  <meta name="viewport" content="width=device-width, initial-scale=1.0">
                  <title>{{title}}</title>
                </head>
                <body style="margin: 0; padding: 0; background-color: #eef2ff; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, sans-serif; -webkit-font-smoothing: antialiased;">
                  <span style="display: none !important; visibility: hidden; opacity: 0; color: transparent; height: 0; width: 0;">Your verification code is {{code}}</span>
                  <table border="0" cellpadding="0" cellspacing="0" width="100%" style="background-color: #eef2ff; padding: 40px 10px;">
                    <tr>
                      <td align="center">
                        <table border="0" cellpadding="0" cellspacing="0" width="100%" style="max-width: 600px; background-color: #fafbff; border-radius: 16px; border: 1px solid #d8dcf8; overflow: hidden; box-shadow: 0 12px 40px rgba(79,70,229,.08);">

                          <tr>
                            <td height="6" style="background-color: #4f46e5; line-height: 6px; font-size: 6px;">&nbsp;</td>
                          </tr>

                          <tr>
                            <td align="center" style="padding: 40px 40px 20px 40px;">
                              <table border="0" cellpadding="0" cellspacing="0" width="100%">
                                <tr>
                                  <td align="center">
                                    <div style="background-color: #eef2ff; width: 56px; height: 56px; border-radius: 14px; display: inline-block; text-align: center; line-height: 56px; border: 1px solid #c7d2fe;">
                                      <span style="font-size: 26px; color: #4f46e5; line-height: 56px;">🔑</span>
                                    </div>
                                  </td>
                                </tr>
                              </table>
                            </td>
                          </tr>

                          <tr>
                            <td align="center" style="padding: 0 40px 20px 40px;">
                              <h1 style="margin: 0; font-size: 24px; font-weight: 800; color: #111827; letter-spacing: -0.5px; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, sans-serif; text-align: center;">{{title}}S</h1>
                            </td>
                          </tr>

                          <tr>
                            <td style="padding: 0 40px 20px 40px; font-size: 15px; line-height: 24px; color: #475569; text-align: left; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, sans-serif;">
                              <p style="text-align: center;">{{description}}</p>
                              <p style="margin: 0 0 16px 0; text-align: center;">This code expires in {{expiryMinutes}} minutes.</p>

                              <div style="text-align: center; padding: 10px 0 20px 0;">
                                <table border="0" cellpadding="0" cellspacing="0" align="center" style="background-color: #eef2ff; border-radius: 12px; border: 1px solid #c7d2fe; display: inline-block;">
                                  <tr>
                                    <td align="center" style="padding: 20px 40px; letter-spacing: 8px;">
                                      <span style="font-family: 'Courier New', Courier, monospace; font-size: 36px; font-weight: 900; color: #4338ca;">{{code}}</span>
                                    </td>
                                  </tr>
                                </table>
                              </div>
                            </td>
                          </tr>

                          <tr>
                            <td align="center" style="padding: 30px 40px; background-color: #f8faff; border-top: 1px solid #e5e7eb;">
                              <p style="margin: 0; font-size: 11px; color: #64748b; line-height: 18px; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, sans-serif; text-align: center;">
                                You received this security email because a request was made using your email address. If you didn't make this request, you can safely ignore this email.
                              </p>
                            </td>
                          </tr>

                        </table>
                      </td>
                    </tr>
                  </table>
                </body>
                </html>
                """;
        }
    }
}
