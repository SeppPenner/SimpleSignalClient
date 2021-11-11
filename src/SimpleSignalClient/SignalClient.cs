namespace SimpleSignalClient;

/// <summary>
/// A client to send messages to Signal.
/// </summary>
public class SignalClient
{
    private readonly TrustStore trustStore = new MyTrustStoreImpl();
    private readonly string userName = "+14151231234";
    private readonly string password;
    private readonly string userAgent;

    private IdentityKeyPair identityKey;
    private List<PreKeyRecord> oneTimePreKeys;
    private SignedPreKeyRecord signedPreKeyRecord;

    /// <summary>
    /// The Signal service urls.
    /// </summary>
    private SignalServiceUrl[] signalServiceUrls = new SignalServiceUrl[] { new SignalServiceUrl("https://chat.signal.org") };

    /// <summary>
    /// The Signal CDN urls.
    /// </summary>
    private SignalCdnUrl[] signalCdnUrls = new SignalCdnUrl[] { new SignalCdnUrl("https://cdn.signal.org") };

    /// <summary>
    /// The Signal CDN 2 urls.
    /// </summary>
    private SignalCdnUrl[] signalCdn2Urls = new SignalCdnUrl[] { new SignalCdnUrl("https://cdn2.signal.org") };

    /// <summary>
    /// The Signal contact discovery urls.
    /// </summary>
    private SignalContactDiscoveryUrl[] signalContactDiscoveryUrls = new SignalContactDiscoveryUrl[] { new SignalContactDiscoveryUrl("https://api.directory.signal.org") };

    /// <summary>
    /// Initializes a new instance of the <see cref="SignalClient"/> class.
    /// </summary>
    /// <param name="userName">The user name.</param>
    /// <param name="password">The password.</param>
    /// <param name="userAgent">The user agent.</param>
    public SignalClient(string userName, string password, string userAgent)
    {
        this.userName = userName;
        this.password = password;
        this.userAgent = userAgent;
    }

    /// <summary>
    /// Sends a message.
    /// </summary>
    public async Task SendMessage()
    {
        // Todo: Abstract message sending here
    }

    private void CreateKeys()
    {
        this.identityKey = KeyHelper.generateIdentityKeyPair();
        this.oneTimePreKeys = KeyHelper.generatePreKeys(0, 100).ToList();
        this.signedPreKeyRecord = KeyHelper.generateSignedPreKey(identityKey, signedPreKeyId);
    }

    private void Register()
    {
        SignalServiceAccountManager accountManager = new SignalServiceAccountManager(signalServerUrl, this.trustStore, this.userName, this.password, this.userAgent);
        accountManager.RequestSmsVerificationCodeAsync();
        accountManager.VerifyAccountWithCodeAsync(receivedSmsVerificationCode, generateRandomSignalingKey(), generateRandomInstallId(), false);
        accountManager.SetGcmIdAsync(Optional.of(GoogleCloudMessaging.getInstance(this).register(REGISTRATION_ID)));
        accountManager.SetPreKeysAsync(this.identityKey.getPublicKey(), this.signedPreKeyRecord, this.oneTimePreKeys);
    }

    /// toAddress: +14159998888
    private void SendMessage(string message, string toAddress)
    {
        var configuration = new SignalServiceConfiguration(this.signalServiceUrls, this.signalCdnUrls, this.signalCdn2Urls, this.signalContactDiscoveryUrls);
        SignalServiceMessageSender messageSender = new SignalServiceMessageSender(configuration, this.trustStore, this.userName, this.password, new MySignalProtocolStore(), this.userAgent, Optional.absent());

        var signalMessage = new SignalServiceDataMessage(DateTimeOffset.Now.ToUnixTimeMilliseconds(), message);
        messageSender.SendMessageAsync(new SignalServiceAddress(toAddress), signalMessage);
    }

    /// toAddress: +14159998888
    /// attachmentPath: /path/to/my.attachment
    private void SendMediaMessage(string message, string toAddress, string attachmentPath)
    {
        SignalServiceMessageSender messageSender = new SignalServiceMessageSender(signalServerUrl, this.trustStore, this.userName, this.password, new MySignalProtocolStore(), this.userAgent, Optional.absent());

        File myAttachment = new File(attachmentPath);
        FileInputStream attachmentStream = new FileInputStream(myAttachment);
        SignalServiceAttachment attachment = SignalServiceAttachment.newStreamBuilder()
                                                                        .withStream(attachmentStream)
                                                                        .withContentType("image/png")
                                                                        .withLength(myAttachment.length())
                                                                        .build();

        messageSender.sendMessage(new SignalServiceAddress(toAddress),
                                SignalServiceDataMessage.newBuilder()
                                                        .withBody(message)
                                                        .withAttachment(attachment)
                                                        .build());
    }

    private void ReceiveMessage()
    {
        SignalServiceMessageReceiver messageReceiver = new SignalServiceMessageReceiver(signalServerUrl, this.trustStore, this.userName, this.password, mySignalingKey, this.userAgent);
        SignalServiceMessagePipe messagePipe = null;

        try
        {
            messagePipe = messageReceiver.CreateMessagePipeAsync();

            while (listeningForMessages)
            {
                SignalServiceEnvelope envelope = messagePipe.Read(timeout, timeoutTimeUnit);
                SignalServiceCipher cipher = new SignalServiceCipher(new SignalServiceAddress(this.userName), new MySignalProtocolStore());
                SignalServiceContent message = cipher.Decrypt(envelope);
                Console.WriteLine("Received message: " + message.Message.Body);
            }
        }
        finally
        {
            if (messagePipe != null)
            {
                messagePipe.Shutdown();
            }
        }
    }
}
