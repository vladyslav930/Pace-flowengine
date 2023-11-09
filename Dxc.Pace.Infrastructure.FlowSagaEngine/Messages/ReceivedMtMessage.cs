using System.Linq;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.Messages
{
    public class ReceivedMtMessage
    {
        public string FaultAddress { get; set; }
        public FlowRequest Message { get; set; }
        public string[] MessageType { get; set; }

        public string GetMissingConsumerTypeName()
        {
            var message = MessageType.FirstOrDefault() ?? "";
            var typeName = GetStringBetween(message, "[[", "]]");
            return typeName.Replace(":", ".");
        }

        private static string GetStringBetween(string str, string firstString, string lastString)
        {
            var pos1 = str.IndexOf(firstString);
            if (pos1 == -1) return null;

            pos1 += firstString.Length;

            var pos2 = str.Substring(pos1).IndexOf(lastString);
            if (pos2 == -1) return null;

            return str.Substring(pos1, pos2);
        }
    }
}
