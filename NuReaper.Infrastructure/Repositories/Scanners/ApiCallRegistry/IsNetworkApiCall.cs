using dnlib.DotNet;
using Microsoft.Extensions.Logging;
using NuReaper.Infrastructure.Repositories.Scanners.ApiCallRegistry.Interfaces;

namespace NuReaper.Infrastructure.Repositories.Scanners.ApiCallRegistry
{
    public class IsNetworkApiCall : IIsNetworkApiCall
    {
        private readonly ILogger<IsNetworkApiCall> _logger;

        public IsNetworkApiCall(ILogger<IsNetworkApiCall> logger)        
        {
            _logger = logger;
        }

         private static readonly string[] NetworkApiCalls = new[]
        {
            // ========== HTTP/HTTPS ==========
            // HttpClient
            "HttpClient::GetAsync",
            "HttpClient::PostAsync",
            "HttpClient::PutAsync",
            "HttpClient::DeleteAsync",
            "HttpClient::SendAsync",
            "HttpClient::GetStringAsync",
            "HttpClient::GetByteArrayAsync",
            "HttpClient::GetStreamAsync",
            "HttpRequestMessage::.ctor",
            
            // HttpWebRequest / WebRequest (legacy)
            "WebRequest::Create",
            "HttpWebRequest::GetResponse",
            "HttpWebRequest::BeginGetResponse",
            "HttpWebRequest::GetRequestStream",
            "HttpWebRequest::BeginGetRequestStream",
            "HttpWebResponse::GetResponseStream", 
            
            // WebClient (legacy but common in malware)
            "WebClient::DownloadString",
            "WebClient::DownloadStringAsync",
            "WebClient::DownloadData",
            "WebClient::DownloadDataAsync",
            "WebClient::DownloadFile",
            "WebClient::DownloadFileAsync",
            "WebClient::OpenRead",
            "WebClient::OpenReadAsync",
            "WebClient::UploadData",           
            "WebClient::UploadDataAsync",
            "WebClient::UploadString",        
            "WebClient::UploadStringAsync",
            "WebClient::UploadFile",          
            "WebClient::UploadFileAsync",
            "WebClient::UploadValues",         
            "WebClient::UploadValuesAsync",

            // ========== DNS ==========
            "Dns::GetHostEntry",
            "Dns::GetHostAddresses",
            "Dns::BeginGetHostEntry",
            "Dns::BeginGetHostAddresses",
            "Dns::GetHostEntryAsync",
            "Dns::GetHostAddressesAsync",
            "Dns::GetHostByName",              

            // ========== TCP ==========
            "TcpClient::Connect",
            "TcpClient::ConnectAsync",
            "TcpClient::BeginConnect",
            "TcpClient::GetStream",
            "TcpListener::Start",
            "TcpListener::Stop",
            "TcpListener::AcceptTcpClient",
            "TcpListener::AcceptTcpClientAsync",
            "TcpListener::AcceptSocket",

            // ========== UDP ==========
            "UdpClient::Connect",
            "UdpClient::Send",
            "UdpClient::SendAsync",
            "UdpClient::Receive",
            "UdpClient::ReceiveAsync",
            "UdpClient::BeginSend",
            "UdpClient::BeginReceive",

            // ========== RAW SOCKETS ==========
            "Socket::Connect",
            "Socket::ConnectAsync",
            "Socket::BeginConnect",
            "Socket::Bind",
            "Socket::Listen",
            "Socket::Accept",
            "Socket::AcceptAsync",
            "Socket::Send",
            "Socket::SendAsync",
            "Socket::SendTo",
            "Socket::SendToAsync",
            "Socket::Receive",
            "Socket::ReceiveAsync",
            "Socket::ReceiveFrom",
            "Socket::ReceiveFromAsync",

            // ========== STREAMS (data exfiltration) ==========
            "NetworkStream::Write",
            "NetworkStream::WriteAsync",
            "NetworkStream::Read",
            "NetworkStream::ReadAsync",
            "Stream::CopyTo",                 
            "Stream::CopyToAsync",

            // ========== SSL/TLS ==========
            "SslStream::AuthenticateAsClient",
            "SslStream::AuthenticateAsClientAsync",
            "SslStream::AuthenticateAsServer",
            "SslStream::AuthenticateAsServerAsync",
            "SslStream::Read",
            "SslStream::ReadAsync",
            "SslStream::Write",
            "SslStream::WriteAsync",
            
            // CRITICAL: Certificate validation bypasses (malware standard)
            "ServicePointManager::set_ServerCertificateValidationCallback",  
            "ServicePointManager::set_SecurityProtocol",                     

            // ========== WebSockets ==========
            "ClientWebSocket::ConnectAsync",
            "ClientWebSocket::SendAsync",
            "ClientWebSocket::ReceiveAsync",
            "ClientWebSocket::CloseAsync",
            "WebSocket::SendAsync",
            "WebSocket::ReceiveAsync",

            // ========== FTP ==========
            "FtpWebRequest::GetResponse",
            "FtpWebRequest::GetRequestStream",
            "FtpWebRequest::BeginGetResponse",

            // ========== SMTP / Mail ==========
            "SmtpClient::Send",
            "SmtpClient::SendAsync",
            "SmtpClient::SendMailAsync",
            "MailMessage::.ctor",

            // ========== Named Pipes (local/remote C2) ==========
            "NamedPipeClientStream::Connect",          
            "NamedPipeClientStream::ConnectAsync",
            "NamedPipeServerStream::WaitForConnection",
            "NamedPipeServerStream::WaitForConnectionAsync",

            // ========== HTTP Handlers / Configuration ==========
            "HttpClientHandler::set_Proxy",            
            "WebProxy::.ctor",                         
            "HttpClientHandler::set_ServerCertificateCustomValidationCallback",  

            // ========== gRPC ==========
            "GrpcChannel::ForAddress",                 
            "CallInvoker::AsyncUnaryCall",

            // ========== MQTT (IoT malware) ==========
            "MqttFactory::CreateMqttClient",
            "MqttClient::ConnectAsync",
            "MqttClient::PublishAsync",
            "MqttClient::SubscribeAsync",

            // ========== RabbitMQ ==========
            "ConnectionFactory::CreateConnection",
            "IModel::BasicPublish",
            "IModel::BasicConsume",

            // ========== SignalR (real-time communication) ==========
            "HubConnection::StartAsync",               
            "HubConnection::SendAsync",
            "HubConnectionBuilder::WithUrl",

            // ========== P/Invoke (Native Windows APIs) ==========
            "InternetOpenA",                           
            "InternetOpenW",
            "InternetConnectA",
            "InternetConnectW",
            "InternetOpenUrlA",
            "InternetOpenUrlW",
            "HttpOpenRequestA",
            "HttpOpenRequestW",
            "HttpSendRequestA",
            "HttpSendRequestW",
            "WSAStartup",                             
            "socket",
            "connect",
            "send",
            "recv",
            "WinHttpOpen",                             
            "WinHttpConnect",
            "WinHttpOpenRequest",
            "WinHttpSendRequest",
        };

        public bool Execute(string methodFullName)
        {
            _logger.LogTrace("[IsNetworkApiCall] Checking if '{MethodFullName}' is a network API call...", methodFullName);
            bool isNetworkApiCall = NetworkApiCalls.Any(api => methodFullName.Contains(api));
            _logger.LogTrace("[IsNetworkApiCall] Result: {IsNetworkApiCall}", isNetworkApiCall);
            return isNetworkApiCall;
        }
    }
}