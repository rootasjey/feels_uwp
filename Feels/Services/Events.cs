namespace Feels.Services {
    public static class Events {
        public static bool IsBackOrEscapeKey(Windows.System.VirtualKey key) {
            return key == Windows.System.VirtualKey.Back ||
                key == Windows.System.VirtualKey.Escape;
        }
    }
}