namespace ReportesDePaqueteria.MVVM.Models
{
    public sealed class Option
    {
        public int Id { get; }
        public string Label { get; }

        public Option(int id, string label)
        {
            Id = id;
            Label = label ?? string.Empty;
        }

        public override string ToString() => Label;
    }
}
