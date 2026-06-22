using Microsoft.Extensions.Logging;

namespace NuReaper.Infrastructure.Repositories.Scanners.ApiCallRegistry
{
    public static class IsNetworkApiCall
    {
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
        private static readonly HashSet<string> NetworkApiCallsSet;
        static IsNetworkApiCall()
        {
            NetworkApiCallsSet = new HashSet<string>(NetworkApiCalls, StringComparer.OrdinalIgnoreCase);
        }
        public static bool Execute(string methodFullName)
        {
            if (string.IsNullOrEmpty(methodFullName))
            {
                Console.WriteLine("Method full name is null or empty.");
                return false;
            }

            if (NetworkApiCallsSet.Contains(methodFullName))
            {
                Console.WriteLine("Method full name is a known network API call: " + methodFullName);
                return true;                
            }

            int parenIndex = methodFullName.IndexOf('(');
            string withoutParams = parenIndex >= 0
                ? methodFullName.Substring(0, parenIndex)
                : methodFullName;

            int lastSeparator = withoutParams.LastIndexOf("::", StringComparison.Ordinal);
            if (lastSeparator != -1)
            {
                // Extract just the method name (after ::)
                string cleanMethodName = withoutParams.Substring(lastSeparator + 2);
                if (NetworkApiCallsSet.Contains(cleanMethodName))
                    return true;

                // Extract "ClassName::MethodName" (last two segments before params)
                string afterReturnType = withoutParams;
                int spaceBeforeClass = withoutParams.LastIndexOf(' ');
                if (spaceBeforeClass >= 0)
                    afterReturnType = withoutParams.Substring(spaceBeforeClass + 1);

                // e.g. "Process::Start"
                string classAndMethod = afterReturnType;
                int classStart = afterReturnType.LastIndexOf('.', lastSeparator - (withoutParams.Length - afterReturnType.Length));

                // Build "ShortClass::Method" for sink matching
                // Walk back to find the last dot before ::
                int separatorInShort = afterReturnType.LastIndexOf("::", StringComparison.Ordinal);
                if (separatorInShort > 0)
                {
                    int dotBeforeClass = afterReturnType.LastIndexOf('.', separatorInShort - 1);
                    string shortForm = dotBeforeClass >= 0
                        ? afterReturnType.Substring(dotBeforeClass + 1)
                        : afterReturnType;

                    if (NetworkApiCallsSet.Contains(shortForm))
                    {
                        Console.WriteLine("Method full name is a known network API call (short form): " + methodFullName);
                        return true;
                    }
                }
            }
            Console.WriteLine("Method full name is not a known network API call: " + methodFullName);
            return false;
        }
    }
}