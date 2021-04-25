using System.Runtime.CompilerServices;
using System.Text;

[assembly: InternalsVisibleTo("TextCycler.Tests")]
namespace TextCycler
{
    public interface IFile
    {
        bool Exists(string path);
        void WriteAllText(string path, string contents);
        void WriteAllText(string path, string contents, Encoding encoding);
        void WriteAllBytes(string path, byte[] bytes);
    }
}
