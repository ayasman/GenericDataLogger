namespace AYLib.GenericDataLogger
{
    /// <summary>
    /// Static provider class for instances of a serialization provider to use in the system.
    /// The built in types always use MessagePack, so the default is set at start and cannot be changed.
    /// </summary>
    public abstract class SerializeProvider
    {
        private static ISerializeProvider currentProvider = null;
        private static ISerializeProvider defaultProvider = null;

        /// <summary>
        /// The currently set provider. Defaults to MessagePackSerializeProvider.
        /// </summary>
        public static ISerializeProvider CurrentProvider => currentProvider;

        /// <summary>
        /// The default provider, is always MessagePackSerializeProvider.
        /// </summary>
        public static ISerializeProvider DefaultProvider => defaultProvider;

        /// <summary>
        /// Creates the default MessagePackSerializeProvider intance, sets it to current.
        /// </summary>
        static SerializeProvider()
        {
            defaultProvider = new MessagePackSerializeProvider();

            SetProvider(DefaultProvider);
        }

        /// <summary>
        /// Sets the current provider to a new provider.
        /// </summary>
        /// <param name="newProvider"></param>
        public static void SetProvider(ISerializeProvider newProvider)
        {
            currentProvider = newProvider;
        }
    }
}
