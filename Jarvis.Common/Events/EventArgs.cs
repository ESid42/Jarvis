using System.ComponentModel;

namespace Jarvis.Utils
{
    public class EventArgs<T>
    {
        public EventArgs(T value)
        {
            Value = value;
        }

        public T Value { get; set; }
    }
}