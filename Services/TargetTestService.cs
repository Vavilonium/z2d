using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using z2d.Models;

namespace z2d.Services
{
    public sealed class TargetTestService
    {
        public async Task<TargetTestResult> TestAsync(
            TargetItem target,
            int timeoutSeconds,
            CancellationToken cancellationToken = default)
        {
            return target.Type switch
            {
                TargetType.Url => await TestUrlAsync(target, timeoutSeconds, cancellationToken),
                TargetType.Ping => await TestPingOnlyAsync(target, timeoutSeconds, cancellationToken),
                _ => throw new ArgumentOutOfRangeException(nameof(target.Type), target.Type, null)
            };
        }

        private async Task<TargetTestResult> TestUrlAsync(
            TargetItem target,
            int timeoutSeconds,
            CancellationToken cancellationToken)
        {
            var host = GetHostFromUrl(target.Target);

            var dnsResult = await CheckDnsAsync(host, timeoutSeconds, cancellationToken);
            var httpResult = await CheckHttpAsync(target.Target, timeoutSeconds, cancellationToken);
            var tls12Result = await CheckTlsAsync(host, SslProtocols.Tls12, timeoutSeconds, cancellationToken);
            var tls13Result = await CheckTlsAsync(host, SslProtocols.Tls13, timeoutSeconds, cancellationToken);
            var pingResult = await CheckPingAsync(host, timeoutSeconds, cancellationToken);

            return new TargetTestResult
            {
                Target = target,
                DnsResolve = dnsResult,
                Http = httpResult,
                Tls12 = tls12Result,
                Tls13 = tls13Result,
                Ping = pingResult
            };
        }

        private async Task<TargetTestResult> TestPingOnlyAsync(
            TargetItem target,
            int timeoutSeconds,
            CancellationToken cancellationToken)
        {
            var pingResult = await CheckPingAsync(target.Target, timeoutSeconds, cancellationToken);

            return new TargetTestResult
            {
                Target = target,
                DnsResolve = TestCheckResult.NotApplicable(),
                Http = TestCheckResult.NotApplicable(),
                Tls12 = TestCheckResult.NotApplicable(),
                Tls13 = TestCheckResult.NotApplicable(),
                Ping = pingResult
            };
        }


        private static async Task<TestCheckResult> CheckDnsAsync(
            string host,
            int timeoutSeconds,
            CancellationToken cancellationToken)
        {
            try
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

                var addresses = await Dns.GetHostAddressesAsync(host, timeoutCts.Token);

                if (addresses.Length == 0)
                {
                    return new TestCheckResult
                    {
                        Status = TestCheckStatus.Failed,
                        Details = "No DNS records"
                    };
                }

                return new TestCheckResult
                {
                    Status = TestCheckStatus.Success,
                    Details = string.Join(", ", addresses.Select(x => x.ToString()))
                };
            }
            catch (OperationCanceledException)
            {
                return new TestCheckResult
                {
                    Status = TestCheckStatus.Timeout,
                    Details = "DNS timeout"
                };
            }
            catch (Exception ex)
            {
                return new TestCheckResult
                {
                    Status = TestCheckStatus.Error,
                    Details = ex.Message
                };
            }
        }

        private static async Task<TestCheckResult> CheckHttpAsync(
            string url,
            int timeoutSeconds,
            CancellationToken cancellationToken)
        {
            try
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

                using var httpClient = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(timeoutSeconds),
                };

                using var request = new HttpRequestMessage(HttpMethod.Head, url);
                using var response = await httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    timeoutCts.Token);

                var statusCode = (int)response.StatusCode;

                return new TestCheckResult
                {
                    Status = TestCheckStatus.Success,
                    Details = $"{statusCode} {response.ReasonPhrase}"
                };
            }
            catch (OperationCanceledException)
            {
                return new TestCheckResult
                {
                    Status = TestCheckStatus.Timeout,
                    Details = "HTTP timeout"
                };
            }
            catch (HttpRequestException ex)
            {
                return new TestCheckResult
                {
                    Status = TestCheckStatus.Failed,
                    Details = ex.Message
                };
            }
            catch (Exception ex)
            {
                return new TestCheckResult
                {
                    Status = TestCheckStatus.Error,
                    Details = ex.Message
                };
            }
        }

        private static async Task<TestCheckResult> CheckTlsAsync(
            string host,
            SslProtocols sslProtocol,
            int timeoutSeconds,
            CancellationToken cancellationToken)
        {
            try
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

                using var tcpClient = new TcpClient();

                await tcpClient.ConnectAsync(host, 443, timeoutCts.Token);

                await using var networkStream = tcpClient.GetStream();

                using var sslStream = new SslStream(
                    networkStream,
                    leaveInnerStreamOpen: false,
                    userCertificateValidationCallback: (_, _, _, _) => true);

                var options = new SslClientAuthenticationOptions
                {
                    TargetHost = host,
                    EnabledSslProtocols = sslProtocol
                };

                await sslStream.AuthenticateAsClientAsync(options, timeoutCts.Token);

                return new TestCheckResult
                {
                    Status = TestCheckStatus.Success,
                    Details = $"{sslStream.SslProtocol} OK"
                };
            }
            catch (OperationCanceledException)
            {
                return new TestCheckResult
                {
                    Status = TestCheckStatus.Timeout,
                    Details = $"{sslProtocol} timeout"
                };
            }
            catch (AuthenticationException ex)
            {
                return new TestCheckResult
                {
                    Status = TestCheckStatus.Failed,
                    Details = ex.Message
                };
            }
            catch (SocketException ex)
            {
                return new TestCheckResult
                {
                    Status = TestCheckStatus.Failed,
                    Details = ex.Message
                };
            }
            catch (NotSupportedException ex)
            {
                return new TestCheckResult
                {
                    Status = TestCheckStatus.Unsupported,
                    Details = ex.Message
                };
            }
            catch (Exception ex)
            {
                return new TestCheckResult
                {
                    Status = TestCheckStatus.Error,
                    Details = ex.Message
                };
            }
        }

        private static async Task<TestCheckResult> CheckPingAsync(
            string target,
            int timeoutSeconds,
            CancellationToken cancellationToken)
        {
            try
            {
                using var ping = new Ping();

                var timeoutMs = (int)TimeSpan.FromSeconds(timeoutSeconds).TotalMilliseconds;
                var reply = await ping.SendPingAsync(target, timeoutMs);

                cancellationToken.ThrowIfCancellationRequested();

                if (reply.Status == IPStatus.Success)
                {
                    return new TestCheckResult
                    {
                        Status = TestCheckStatus.Success,
                        Details = $"{reply.RoundtripTime} ms"
                    };
                }

                return new TestCheckResult
                {
                    Status = reply.Status == IPStatus.TimedOut
                        ? TestCheckStatus.Timeout
                        : TestCheckStatus.Failed,
                    Details = reply.Status.ToString()
                };
            }
            catch (OperationCanceledException)
            {
                return new TestCheckResult
                {
                    Status = TestCheckStatus.Timeout,
                    Details = "Ping canceled or timed out"
                };
            }
            catch (Exception ex)
            {
                return new TestCheckResult
                {
                    Status = TestCheckStatus.Error,
                    Details = ex.Message
                };
            }
        }

        private static string GetHostFromUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out var uri)
                ? uri.Host
                : url;
        }
    }
}
