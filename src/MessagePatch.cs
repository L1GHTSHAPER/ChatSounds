using HarmonyLib;

namespace ChatSounds
{
    /// <summary>
    /// The game calls AddMessageUI for every chat line it shows: with sendByPlayer = true for your own messages, and
    /// with false for another player's message that passed its filters (ignored and muted players, local chat
    /// distance). So a postfix sees exactly the messages that actually appeared in the chat.
    /// </summary>
    [HarmonyPatch(typeof(TextChannelManager), "AddMessageUI")]
    internal static class MessagePatch
    {
        [HarmonyPostfix]
        static void Postfix(string text, bool isLocal, bool sendByPlayer)
        {
            if (sendByPlayer)
                return;
            Plugin plugin = Plugin.Instance;
            if (plugin != null)
                plugin.OnMessageShown(text, isLocal);
        }
    }
}
