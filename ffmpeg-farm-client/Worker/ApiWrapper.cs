using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FFmpegFarm.Worker.Client;
using Nito.AsyncEx.Synchronous;

namespace FFmpegFarm.Worker
{
    public class ApiWrapper : IApiWrapper
    {
        private CancellationToken _cancellationToken;
        private readonly ILogger _logger;
        public int? ThreadId { get; set; }

        private readonly StatusClient _statusClient;
        private readonly TaskClient _taskClient;

        public ApiWrapper(string apiUri, ILogger logger, CancellationToken ct)
        {
            _taskClient = new TaskClient(apiUri);
            _statusClient = new StatusClient(apiUri);
            _logger = logger;
            _cancellationToken = ct;
        }

        public FFmpegTaskDto GetNext(string machineName)
        {
            return Wrap(_taskClient.GetNextAsync, machineName);
        }

        public void UpdateProgress(TaskProgressModel model, bool ignoreCancel = false)
        {
            if (ignoreCancel)
                // don't use wrapper since cancel has been called. 
                _statusClient.UpdateProgressAsync(model, CancellationToken.None)
                    .WaitWithoutException(CancellationToken.None);
            else
                Wrap(_statusClient.UpdateProgressAsync, model);
        }

        /// <summary>
        /// Retries and ignores exceptions.
        /// </summary>
        private TRes Wrap<TRes>(Func<TRes> func)
        {
            const int retryCount = 3;
            Exception exception = null;
            SwaggerException swaggerException = null;
            for (var x = 0; !_cancellationToken.IsCancellationRequested && x < retryCount; x++)
            {
#if DEBUGAPI
                var timer = new Stopwatch();
                timer.Start();
#endif
                try
                {
                    return func();
                }
                catch (Exception e)
                {
                    exception = e;
                    swaggerException = e as SwaggerException;
#if DEBUGAPI
                    if (swaggerException != null)
                        _logger.Warn($"{swaggerException.StatusCode} : {Encoding.UTF8.GetString(swaggerException.ResponseData)}", _threadId);
                    _logger.Exception(e, _threadId);
#endif
                }
#if DEBUGAPI
                finally
                {
                    _logger.Debug($"API call took {timer.ElapsedMilliseconds} ms", _threadId);
                    timer.Stop();
                }
#endif
                Task.Delay(TimeSpan.FromSeconds(1), _cancellationToken).GetAwaiter().GetResult();
            }
            if (swaggerException != null)
                _logger.Warn($"{swaggerException.StatusCode} : {Encoding.UTF8.GetString(swaggerException.ResponseData)}", ThreadId);
            _logger.Exception(exception ?? new Exception(nameof(ApiWrapper)), ThreadId);
            return default(TRes);
        }


        /// <summary>
        /// Retries and ignores exceptions.
        /// </summary>
        private TRes Wrap<TArg, TRes>(Func<TArg, CancellationToken, Task<TRes>> apiCall, TArg arg)
        {
            return Wrap(() => apiCall(arg, CancellationToken.None).WaitAndUnwrapException());
        }

        /// <summary>
        /// Retries and ignores exceptions.
        /// </summary>
        private void Wrap<TArg>(Func<TArg, CancellationToken, Task> apiCall, TArg arg)
        {
            Wrap(
                new Func<object>(() => {
                    apiCall(arg, CancellationToken.None).WaitAndUnwrapException();
                    return null;
                }));
        }
    }
}
