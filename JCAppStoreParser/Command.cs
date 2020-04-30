using System;

namespace JCAppStore_Parser
{
    public class Command : IComparable<Command>
    {
        public string Name { get; private set; }
        public string Description { get; private set; }
        public string Arg { get; private set; }
        public string ArgValue { get; set; }

        public Command(string cmd, string descritpion, string arg = null)
        {
            Name = cmd;
            Description = descritpion;
            Arg = arg;
        }

        public override string ToString()
        {
            return $"{Name,10} {GetDescription()}";
        }

        public string GetDescription()
        {
            return $"{Description} {(Arg != null ? $"\r\n\tRequires argument: {Arg}" : "")}";
        }

        public static Command FromString(string value)
        {
            if (value == null || value.Length < 2 || value[0] != '-') return null;
            return new Command(value, "", "");
        }

        public int CompareTo(Command other)
        {
            return Name.CompareTo(other.Name);
        }

        public override bool Equals(object y)
        {
            if (y == null || !(y is Command))
            {
                return false;
            }
            return CompareTo((Command)y) == 0;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
    }
}
