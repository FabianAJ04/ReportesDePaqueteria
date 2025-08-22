

namespace ReportesDePaqueteria.MVVM.ViewModels
{
    public static class TrackingGenerator
    {
        private static readonly Random _rng = new();

        public static string NewCode()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var suffix = new string(Enumerable.Range(0, 6)
                .Select(_ => chars[_rng.Next(chars.Length)]).ToArray());
            return $"SHP-{DateTime.UtcNow:yyyyMMdd}-{suffix}";
        }
    }
}