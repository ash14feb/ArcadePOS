using APOS3.DataAccess.Repos;
using Microsoft.Extensions.Logging;
using System.Net.Sockets;
using System.Text;

public class UdpService : IAsyncDisposable
{
    private readonly UdpClient _udp;
    private readonly IRfidService _rfidService;
    private CancellationTokenSource _cts;
    private readonly ILogger<UdpService> _logger;

    public UdpService(IRfidService rfidService, ILogger<UdpService> logger)
    {
        _udp = new UdpClient(9000); // LISTEN on port 9000
        _rfidService = rfidService;
        _logger = logger;
        _cts = new CancellationTokenSource();
        StartListening();
    }

    private void StartListening()
    {
        Task.Run(async () =>
        {
            try
            {
                while (!_cts.IsCancellationRequested)
                {
                    var received = await _udp.ReceiveAsync(_cts.Token);

                    string message = Encoding.UTF8.GetString(received.Buffer);

                    _logger.LogInformation($"📩 Received: {message} from {received.RemoteEndPoint}");

                    // Process RFID message and get appropriate response
                    string reply = await _rfidService.ProcessRfidMessageAsync(message, received.RemoteEndPoint);

                    // Reply back to sender
                    byte[] data = Encoding.UTF8.GetBytes(reply);
                    await _udp.SendAsync(data, data.Length, received.RemoteEndPoint);

                    _logger.LogInformation($"📤 Sent reply: {reply} to {received.RemoteEndPoint}");
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("UDP listener stopped");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UDP listener");
            }
        });
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        _udp.Close();
        _udp.Dispose();
        _cts.Dispose();
    }
}