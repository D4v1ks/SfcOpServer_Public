#pragma warning disable CA1051

namespace SfcOpServer
{
    public class GsProfile
    {
        public int Id;

        public string Email;
        public string Nick;
        public string Password;

        public string Username => Nick + "@" + Email;
    }
}
