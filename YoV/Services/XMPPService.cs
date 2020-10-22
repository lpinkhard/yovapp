using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using YoV.Models;

namespace YoV.Services
{
    public class XMPPService
    {
        private List<Contact> roster;
        private string server;
        private Thread listenThread;
        private Thread processThread;
        private bool threadRunning;
        private Stream listenStream;
        private StreamWriter streamWriter;
        private TcpClient xmppClient;
        private bool openStream;
        private bool connected;
        private SessionState currentState;
        private LoginStatus loginStatus;
        private List<AuthMechanism> authMechanisms;
        private int nextIQ;
        private List<IQRequest> iqRequests;
        private bool resourceBinding;
        private bool usesSessions;
        private bool presenceSent;
        private string jid;
        private RosterStatus rosterStatus;
        private Queue<string> requestQueue;
        private List<Message> messages;
        private static readonly object writeLock = new object();
        private long lastKeepalive;

        private enum AuthMechanism
        {
            PLAIN,
            X_OAUTH2,
            DIGEST_MD5,
            SCRAM_SHA_1
        }

        private enum SessionState
        {
            DISCONNECTED,
            CONNECTED,
            TLS_REQUESTED,
            PRE_AUTH,
            AUTHENTICATED,
            BINDING,
            ESTABLISHING,
            ACTIVE
        }

        private enum LoginStatus
        {
            PENDING,
            SUCCESS,
            FAILURE
        }

        private enum RosterStatus
        {
            READY,
            UPDATING
        }

        private enum IQMessageType
        {
            GET,
            SET
        }

        public XMPPService()
        {
            server = "myyovproto.westus2.cloudapp.azure.com";
            roster = new List<Contact>();
            listenThread = null;
            processThread = null;
            threadRunning = false;
            listenStream = null;
            streamWriter = null;
            xmppClient = new TcpClient();
            openStream = false;
            connected = false;
            currentState = SessionState.DISCONNECTED;
            authMechanisms = new List<AuthMechanism>();
            loginStatus = LoginStatus.PENDING;
            nextIQ = 1;
            iqRequests = new List<IQRequest>();
            resourceBinding = false;
            usesSessions = false;
            jid = "";
            rosterStatus = RosterStatus.READY;
            presenceSent = false;
            requestQueue = new Queue<string>();
            lastKeepalive = DateTimeOffset.Now.ToUnixTimeSeconds();
            messages = new List<Message>();
        }

        private string ReadStanza(StreamReader reader)
        {
            StringBuilder sb = new StringBuilder();
            int elemDepth = 0;
            bool elements = false;
            bool finished = false;
            bool insideTag = false;
            int pch = -1;
            try
            {
                do
                {
                    int ch = reader.Read();
                    if (ch == -1)
                        continue;
                    if (!elements && (ch == ' ' || ch == '\r' || ch == '\n'))
                        continue;

                    switch ((char)ch)
                    {
                        case '<':
                            elemDepth++;
                            elements = true;
                            insideTag = true;
                            break;
                        case '/':
                            if (pch == '<')
                            {
                                elemDepth -= 2;
                            }
                            break;
                        case '>':
                            insideTag = false;
                            if (sb.ToString().Contains("<stream:stream") ||
                                sb.ToString().Contains("<?xml"))
                            {
                                elemDepth--;
                            }
                            else if (pch == '/')
                            {
                                elemDepth--;
                            }
                            break;
                    }

                    if (ch != ' ' && ch != '\n' && ch != '\r')
                        pch = ch;
                    sb.Append((char)ch);

                    if (elements && !insideTag && elemDepth < 1)
                        finished = true;

                    if (!threadRunning)
                        finished = true;
                }
                while (!finished);
            } catch (IOException)
            {
                return null;
            }

            string result = sb.ToString();
            if (result.Contains("<?xml"))
                return ReadStanza(reader);
            else if (result.Contains("<stream:stream"))
            {
                openStream = true;
                return ReadStanza(reader);
            }
            else if (result.Contains("</stream:stream"))
            {
                openStream = false;
                return ReadStanza(reader);
            }

            Debug.WriteLine("S: " + result);

            return result;
        }

        private void WriteStanza(string stanza)
        {
            lock (writeLock)
            {
                requestQueue.Enqueue(stanza);
            }
        }

        private void ProcessQueue()
        {
            do
            {
                /* if (requestQueue.Count == 0)
                {
                    long timeNow = DateTimeOffset.Now.ToUnixTimeSeconds();
                    if (timeNow > lastKeepalive + 5)
                    {
                        lastKeepalive = timeNow;
                        streamWriter.Write("\n");
                        streamWriter.Flush();
                    }
                } */
                while (requestQueue.Count > 0)
                {
                    string stanza = requestQueue.Dequeue();

                    Debug.WriteLine("C: " + stanza);

                    if (streamWriter != null && streamWriter.BaseStream != null
                        && streamWriter.BaseStream.CanWrite)
                    {
                        streamWriter.Write(stanza);
                        streamWriter.Flush();
                    }
                }
                Thread.Sleep(100);
            }
            while (threadRunning);
        }

        private void StreamListener()
        {
            StreamReader reader = new StreamReader(listenStream);

            XmlReaderSettings xmlReaderSettings = new XmlReaderSettings
            {
                NameTable = new NameTable()
            };
            XmlNamespaceManager xmlns =
                new XmlNamespaceManager(xmlReaderSettings.NameTable);
            xmlns.AddNamespace("stream", "http://etherx.jabber.org/streams");
            XmlParserContext xmlContext =
                new XmlParserContext(null, xmlns, "", XmlSpace.Default);

            do
            {
                string stanza = ReadStanza(reader);
                if (stanza == null)
                {
                    threadRunning = false;
                    break;
                }

                XmlReader responseReader =
                XmlReader.Create(
                    new StringReader(stanza), xmlReaderSettings, xmlContext
                );

                switch (currentState)
                {
                    case SessionState.CONNECTED:
                        authMechanisms.Clear();
                        bool startTLS = false;

                        while (responseReader.Read())
                        {
                            if (responseReader.NodeType == XmlNodeType.Element)
                            {
                                if (responseReader.Name.Equals("mechanism"))
                                {
                                    string mechanism =
                                        responseReader.ReadElementContentAsString();
                                    if (mechanism.Equals("PLAIN"))
                                        authMechanisms.Add(AuthMechanism.PLAIN);
                                    else if (mechanism.Equals("X-OAUTH2"))
                                        authMechanisms.Add(AuthMechanism.X_OAUTH2);
                                    else if (mechanism.Equals("DIGEST-MD5"))
                                        authMechanisms.Add(AuthMechanism.DIGEST_MD5);
                                    else if (mechanism.Equals("SCRAM-SHA-1"))
                                        authMechanisms.Add(AuthMechanism.SCRAM_SHA_1);
                                }
                                else if (responseReader.Name.Equals("starttls"))
                                {
                                    //startTLS = true;
                                }
                            }
                        }

                        if (startTLS)
                        {
                            string tlsRequest =
                                "<starttls xmlns='urn:ietf:params:xml:ns:xmpp-tls' />";

                            currentState = SessionState.TLS_REQUESTED;

                            WriteStanza(tlsRequest);
                        }
                        else
                        {
                            currentState = SessionState.PRE_AUTH;
                        }

                        break;
                    case SessionState.TLS_REQUESTED:
                        while (responseReader.Read())
                        {
                            if (responseReader.NodeType == XmlNodeType.Element)
                            {
                                if (responseReader.Name.Equals("proceed"))
                                {
                                    SslStream sslStream = new SslStream(
                                        listenStream, false);

                                    sslStream.AuthenticateAsClient(server);

                                    currentState = SessionState.PRE_AUTH;

                                    reader.Close();
                                    reader = new StreamReader(sslStream);

                                    streamWriter.Close();
                                    streamWriter = new StreamWriter(sslStream);

                                    listenStream = sslStream;
                                }
                            }
                        }

                        break;
                    case SessionState.PRE_AUTH:
                        while (responseReader.Read())
                        {
                            if (responseReader.NodeType == XmlNodeType.Element)
                            {
                                if (responseReader.Name.Equals("success"))
                                {
                                    loginStatus = LoginStatus.SUCCESS;
                                    currentState = SessionState.AUTHENTICATED;
                                    ResetStream();
                                }
                                else if (responseReader.Name.Equals("failure"))
                                {
                                    loginStatus = LoginStatus.FAILURE;
                                    currentState = SessionState.CONNECTED;
                                    ResetStream();
                                }
                            }
                        }

                        break;
                    case SessionState.AUTHENTICATED:
                        resourceBinding = false;
                        usesSessions = false;

                        while (responseReader.Read())
                        {
                            if (responseReader.NodeType == XmlNodeType.Element)
                            {
                                if (responseReader.Name.Equals("bind"))
                                {
                                    resourceBinding = true;
                                }
                                else if (responseReader.Name.Equals("session"))
                                {
                                    usesSessions = true;
                                }
                            }
                        }

                        if (resourceBinding)
                        {
                            IQSet(
                                "<bind xmlns='urn:ietf:params:xml:ns:xmpp-bind' />",
                                OnBind
                            );
                            currentState = SessionState.BINDING;
                        }
                        else if (usesSessions)
                        {
                            currentState = SessionState.ESTABLISHING;

                            IQSet(
                                "<session xmlns='urn:ietf:params:xml:ns:xmpp-session' />",
                                OnEstablish
                            );
                        }
                        else
                        {
                            currentState = SessionState.ACTIVE;
                        }

                        break;
                    case SessionState.BINDING:
                    case SessionState.ESTABLISHING:
                        while (responseReader.Read())
                        {
                            if (responseReader.NodeType == XmlNodeType.Element)
                            {
                                if (responseReader.Name.Equals("iq"))
                                {
                                    handleIQ(responseReader.GetAttribute("id"),
                                        responseReader.GetAttribute("type"),
                                        responseReader.ReadInnerXml());
                                }
                            }
                        }

                        break;
                    case SessionState.ACTIVE:
                        while (responseReader.Read())
                        {
                            if (responseReader.NodeType == XmlNodeType.Element)
                            {
                                if (responseReader.Name.Equals("iq"))
                                {
                                    handleIQ(responseReader.GetAttribute("id"),
                                        responseReader.GetAttribute("type"),
                                        responseReader.ReadInnerXml());
                                }
                                else if (responseReader.Name.Equals("message"))
                                {
                                    handleMessage(responseReader.GetAttribute("from"),
                                        responseReader.GetAttribute("type"),
                                        responseReader.ReadInnerXml());
                                }
                                else if (responseReader.Name.Equals("presence"))
                                {
                                    string presenceType = responseReader.GetAttribute("type");
                                    if (presenceType != null)
                                    {
                                        if (presenceType.Equals("subscribe"))
                                        {
                                            ApproveUser(responseReader.GetAttribute("from"));
                                        }
                                    }
                                }
                            }
                        }

                        break;
                }
            }
            while (threadRunning);

            reader.Close();
        }

        private void ResetStream()
        {
            string streamHead = "<stream:stream to='"
                + server + "' xmlns='jabber:client' "
                + "xmlns:stream='http://etherx.jabber.org/streams' "
                + "version='1.0'>";

            WriteStanza(streamHead);
        }

        private void handleIQ(string msgID, string msgType, string message)
        {
            IQRequest iqRequest = null;

            if (msgType.Equals("result"))
            {
                foreach (IQRequest request in iqRequests)
                {
                    if (request.ID.ToString().Equals(msgID))
                    {
                        iqRequest = request;
                        break;
                    }
                }

                if (iqRequest != null)
                {
                    iqRequest.Output(message);
                    iqRequests.Remove(iqRequest);
                }
            }
        }

        private void handleMessage(string msgSender, string msgType, string message)
        {
            XmlReader xmlReader = XmlReader.Create(new StringReader(
                "<root>" + message + "</root>"));
            while (xmlReader.Read())
            {
                if (xmlReader.NodeType == XmlNodeType.Element)
                {
                    if (xmlReader.Name.Equals("body"))
                    {
                        string content = xmlReader.ReadElementContentAsString();

                        Message newMessage = new Message
                        {
                            User = msgSender.Substring(0, msgSender.IndexOf("@")),
                            Content = content,
                            Direction = Message.MessageDirection.INCOMING
                        };

                        lock (writeLock)
                        {
                            Debug.WriteLine(msgSender);
                            messages.Add(newMessage);
                        }
                    }
                }
            }
        }

        private void StartListen(Stream listenStream)
        {
            this.listenStream = listenStream;

            threadRunning = true;
            listenThread = new Thread(StreamListener);
            listenThread.Start();

            processThread = new Thread(ProcessQueue);
            processThread.Start();
        }

        private void StopListen()
        {
            if (threadRunning)
            {
                threadRunning = false;
                processThread.Join();
                processThread = null;
                listenThread.Join();
                listenThread = null;
            }
        }

        private Task StartConnection()
        {
            if (connected)
            {
                StopConnection();
                xmppClient = new TcpClient();
            }

            xmppClient.Connect(server, 5222);

            currentState = SessionState.CONNECTED;

            streamWriter = new StreamWriter(xmppClient.GetStream());

            StartListen(xmppClient.GetStream());

            WriteStanza("<?xml version='1.0'?>");

            ResetStream();

            connected = true;

            return Task.CompletedTask;
        }

        private Task StopConnection()
        {
            if (connected)
            {
                if (currentState == SessionState.ACTIVE)
                    SendPresence(false);

                string streamTail = "</stream:stream>";

                if (streamWriter != null)
                {
                    WriteStanza(streamTail);

                    streamWriter.Close();
                }

                StopListen();

                if (xmppClient != null)
                {
                    xmppClient.Close();
                }
            }

            requestQueue.Clear();

            return Task.CompletedTask;
        }

        private string GetBase64(string unencoded)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(unencoded));
        }

        private string AuthPlain(string jid, string password)
        {
            return GetBase64('\0' + jid + '\0' + password);
        }

        private void SendPresence(bool available)
        {
            if (available)
                WriteStanza("<presence from='" + jid + "' />");
            else
                WriteStanza("<presence from='" + jid + "' type='unavailable' />");
        }

        private void ApproveUser(string jid)
        {
            WriteStanza("<presence to='" + jid + "' type='subscribed' />");
        }

        private void RefuseUser(string jid)
        {
            WriteStanza("<presence to='" + jid + "' type='unsubscribed' />");
        }

        private void RequestUser(string jid)
        {
            WriteStanza("<presence to='" + jid + "' type='subscribe' />");
        }

        private void IQMessage(IQMessageType messageType, string message,
            Func<string, bool> output)
        {
            int requestID = nextIQ++;

            IQRequest request = new IQRequest
            {
                ID = requestID,
                Output = output
            };

            iqRequests.Add(request);

            string iqHead = "<iq id='" + requestID + "' type='";
            string iqType = null;

            switch (messageType)
            {
                case IQMessageType.GET:
                    iqType = "get";
                    break;
                case IQMessageType.SET:
                    iqType = "set";
                    break;
            }

            string iqMid = "'>";
            if (currentState == SessionState.ACTIVE)
                iqMid = "' from='" + jid + "'>";
            string iqTail = "</iq>";

            WriteStanza(iqHead + iqType + iqMid + message + iqTail);
        }

        private void IQSet(string message, Func<string, bool> output)
        {
            IQMessage(IQMessageType.SET, message, output);
        }

        private void IQGet(string message, Func<string, bool> output)
        {
            IQMessage(IQMessageType.GET, message, output);
        }

        public void Login(string username, string password,
            Func<bool, bool> loginOutput)
        {
            StartConnection();

            int wait = 0;
            bool canLogin = false;
            jid = username + "@" + server;

            // Wait for ready to login
            do
            {
                Thread.Sleep(100);
                if (currentState == SessionState.PRE_AUTH)
                {
                    canLogin = true;
                    loginStatus = LoginStatus.PENDING;
                }
            } while (wait < 600 && !canLogin);
            wait = 0;

            if (canLogin)
            {
                string authHead =
                    "<auth xmlns='urn:ietf:params:xml:ns:xmpp-sasl' mechanism='";
                string authMid =
                    "' xmlns:ga='http://www.google.com/talk/protocol/auth' "
                    + "ga:client-uses-full-bind-results='true'>";
                string authTail =
                    "</auth>";

                string authRequest = null;

                if (authMechanisms.Contains(AuthMechanism.PLAIN))
                {
                    string authKey = AuthPlain(jid, password);
                    authRequest = authHead + "PLAIN" + authMid + authKey + authTail;
                }

                WriteStanza(authRequest);

                // Wait for login outcome
                bool loginSuccess = false;
                do
                {
                    Thread.Sleep(100);
                    if (loginStatus == LoginStatus.SUCCESS)
                        loginSuccess = true;
                } while (wait < 600 && loginStatus == LoginStatus.PENDING);

                loginOutput(loginSuccess);
            }
            else
            {
                StopConnection();
                loginOutput(false);
            }
        }

        private bool OnBind(string result)
        {
            XmlReader xmlReader = XmlReader.Create(new StringReader(result));
            while (xmlReader.Read())
            {
                if (xmlReader.NodeType == XmlNodeType.Element)
                {
                    if (xmlReader.Name.Equals("jid"))
                    {
                        string jid = xmlReader.ReadElementContentAsString();
                        this.jid = jid;
                    }
                }
            }

            if (usesSessions)
            {
                currentState = SessionState.ESTABLISHING;

                IQSet(
                    "<session xmlns='urn:ietf:params:xml:ns:xmpp-session' />",
                    OnEstablish
                );

            }
            else
            {
                currentState = SessionState.ACTIVE;
                SendPresence(true);
            }

            return true;
        }

        private bool OnQueryRoster(string result)
        {
            roster.Clear();

            XmlReader xmlReader = XmlReader.Create(new StringReader(result));
            while (xmlReader.Read())
            {
                if (xmlReader.NodeType == XmlNodeType.Element)
                {
                    if (xmlReader.Name.Equals("item"))
                    {
                        string username = xmlReader.GetAttribute("jid");
                        int split = username.IndexOf('@');
                        if (split > 0)
                            username = username.Substring(0, split);

                        string name = xmlReader.GetAttribute("name");
                        if (name == null)
                        {
                            name = username;
                        }

                        Contact contact = new Contact
                        {
                            DisplayName = name,
                            Username = username
                        };
                        roster.Add(contact);
                    }
                }
            }

            rosterStatus = RosterStatus.READY;
            return true;
        }

        private bool OnEstablish(string result)
        {
            currentState = SessionState.ACTIVE;
            SendPresence(true);

            return true;
        }

        private bool OnEmptyResponse(string result)
        {
            return true;
        }

        public Task<List<Contact>> GetRosterAsync()
        {
            if (currentState == SessionState.ACTIVE)
            {
                rosterStatus = RosterStatus.UPDATING;
                IQGet(
                    "<query xmlns='jabber:iq:roster' />",
                    OnQueryRoster
                );

                int wait = 0;
                do
                {
                    Thread.Sleep(100);
                    wait++;
                } while (rosterStatus != RosterStatus.READY && wait < 600);
            }

            return Task.FromResult(roster);
        }

        public Task<List<Message>> GetMessagesAsync(Contact contact)
        {
            List<Message> contactMessages = new List<Message>();

            foreach (Message msg in messages)
            {
                if (msg.User.Equals(contact.Username))
                {
                    contactMessages.Add(msg);
                }
            }

            return Task.FromResult(contactMessages);
        }

        public void AddContactAsync(Contact contact)
        {
            if (currentState == SessionState.ACTIVE)
            {
                string jid = contact.Username + "@" + server;
                string name = contact.DisplayName;

                IQSet(
                    "<query xmlns='jabber:iq:roster'><item jid='" + jid
                    + "' name='" + name + "' /></query>",
                    OnEmptyResponse
                );

                RequestUser(jid);
            }

            roster.Add(contact);
        }

        public void SendMessageAsync(Message message)
        {
            if (currentState == SessionState.ACTIVE)
            {
                string recipient = message.User + "@" + server;
                string content = message.Content;

                string data = "<message from='" + jid + "' id='" + nextIQ++
                    + "' to='" + recipient + "' type='chat' xml:lang='en'>"
                    + "<body>" + content + "</body></message>";

                lock (writeLock)
                {
                    messages.Add(message);
                }

                WriteStanza(data);
            }
        }

        public class IQRequest
        {
            public int ID { get; set; }
            public Func<string, bool> Output { get; set; }
        }
    }
}
