using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Wihngo.Middleware;

/// <summary>
/// Custom log filter to show only crypto payment related logs
/// </summary>
public class CryptoPaymentLogFilter : ILoggerProvider
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly HashSet<string> _cryptoNamespaces = new()
    {
        "Wihngo.Services.CryptoPaymentService",
        "Wihngo.Services.BlockchainVerificationService",
        "Wihngo.Controllers.CryptoPaymentsController",
        "Wihngo.BackgroundJobs.ExchangeRateUpdateJob",
        "Wihngo.BackgroundJobs.PaymentMonitorJob"
    };

    public CryptoPaymentLogFilter(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new CryptoPaymentLogger(categoryName, _cryptoNamespaces);
    }

    public void Dispose()
    {
    }

    private class CryptoPaymentLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly HashSet<string> _allowedNamespaces;

        public CryptoPaymentLogger(string categoryName, HashSet<string> allowedNamespaces)
        {
            _categoryName = categoryName;
            _allowedNamespaces = allowedNamespaces;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            // Only show logs from crypto-related namespaces
            return _allowedNamespaces.Any(ns => _categoryName.StartsWith(ns));
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var logMessage = formatter(state, exception);
            var logLevelString = logLevel.ToString().ToUpper();

            // Color-coded output
            var color = logLevel switch
            {
                LogLevel.Error or LogLevel.Critical => ConsoleColor.Red,
                LogLevel.Warning => ConsoleColor.Yellow,
                LogLevel.Information => ConsoleColor.Green,
                LogLevel.Debug => ConsoleColor.Cyan,
                _ => ConsoleColor.White
            };

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write($"[{timestamp}] ");
            
            Console.ForegroundColor = color;
            Console.Write($"[{logLevelString}] ");
            
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write($"[CRYPTO] ");
            
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(logMessage);

            if (exception != null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Exception: {exception.Message}");
                Console.WriteLine(exception.StackTrace);
            }

            Console.ResetColor();
        }
    }
}
