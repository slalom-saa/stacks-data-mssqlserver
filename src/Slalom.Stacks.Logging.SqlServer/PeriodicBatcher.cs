// Copyright 2013-2016 Serilog Contributors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// Modifications copyright(C) 2017 Slalom Architect Academy


using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Serilog.Sinks.PeriodicBatching;
using Slalom.Stacks.Messaging.Logging;

namespace Slalom.Stacks.Logging.SqlServer
{
    public abstract class PeriodicBatcher<T> : IDisposable
    {
        private readonly int _batchSizeLimit = 100;

        public ILogger Logger { get; set; }

        readonly object _stateLock = new object();

        readonly BatchedConnectionStatus _status;

        readonly Queue<T> _waitingBatch = new Queue<T>();
        private readonly BoundedConcurrentQueue<T> _queue;
        bool _started;
        private readonly PortableTimer _timer;

        bool _unloading;

        protected virtual async Task EmitBatchAsync(IEnumerable<T> events)
        {
            this.EmitBatch(events);
        }

        protected virtual void EmitBatch(IEnumerable<T> events)
        {
        }

        protected PeriodicBatcher(int batchSize, TimeSpan period)
        {
            _batchSizeLimit = batchSize;
            _queue = new BoundedConcurrentQueue<T>();
            _timer = new PortableTimer(cancel => this.OnTick());
            _status = new BatchedConnectionStatus(period);
        }

        #region IDisposable Implementation

        bool _disposed;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="LogStore"/> class.
        /// </summary>
        ~PeriodicBatcher()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                // free other managed objects that implement IDisposable only

                this.CloseAndFlush();
            }

            // release any unmanaged objects
            // set the object references to null

            _disposed = true;
        }

        #endregion

        void CloseAndFlush()
        {
            lock (_stateLock)
            {
                if (!_started || _unloading)
                    return;

                _unloading = true;
            }

            _timer.Dispose();

            // This is the place where SynchronizationContext.Current is unknown and can be != null
            // so we prevent possible deadlocks here for sync-over-async downstream implementations 
            this.ResetSyncContextAndWait(this.OnTick);
        }

        async Task OnTick()
        {
            try
            {
                bool batchWasFull;
                do
                {
                    T next;
                    while (_waitingBatch.Count < _batchSizeLimit &&
                           _queue.TryDequeue(out next))
                    {
                        _waitingBatch.Enqueue(next);
                    }

                    if (_waitingBatch.Count == 0)
                    {
                        return;
                    }

                    await this.EmitBatchAsync(_waitingBatch);

                    batchWasFull = _waitingBatch.Count >= _batchSizeLimit;
                    _waitingBatch.Clear();
                    _status.MarkSuccess();
                }
                while (batchWasFull); // Otherwise, allow the period to elapse
            }
            catch (Exception ex)
            {
                this.Logger.Error(ex, "Exception while emitting periodic batch from {Instance}", this);
                _status.MarkFailure();
            }
            finally
            {
                if (_status.ShouldDropBatch)
                {
                    _waitingBatch.Clear();
                }

                if (_status.ShouldDropQueue)
                {
                    T evt;
                    while (_queue.TryDequeue(out evt)) { }
                }

                lock (_stateLock)
                {
                    if (!_unloading)
                    {
                        this.SetTimer(_status.NextInterval);
                    }
                }
            }
        }

        void ResetSyncContextAndWait(Func<Task> taskFactory)
        {
            var prevContext = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(null);
            try
            {
                taskFactory().Wait();
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(prevContext);
            }
        }

        void SetTimer(TimeSpan interval)
        {
            _timer.Start(interval);
        }

        public void Emit(T logEvent)
        {
            if (logEvent == null) throw new ArgumentNullException(nameof(logEvent));

            if (_unloading)
                return;

            if (!_started)
            {
                lock (_stateLock)
                {
                    if (_unloading) return;
                    if (!_started)
                    {
                        // Special handling to try to get the first event across as quickly
                        // as possible to show we're alive!
                        _queue.TryEnqueue(logEvent);
                        _started = true;
                        this.SetTimer(TimeSpan.Zero);
                        return;
                    }
                }
            }

            _queue.TryEnqueue(logEvent);
        }

    }
}