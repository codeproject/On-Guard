﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;

namespace OnGuardCore
{

  public class AwaitableQueue<T> : IDisposable
  {
    int _waitTime;
    bool _stop;

    ConcurrentQueue<T> _q = new ConcurrentQueue<T>();
    AsyncAutoResetEvent _available = new AsyncAutoResetEvent(true);
    private bool disposedValue;

    public AwaitableQueue(int waitTimeInSeconds)
    {
      _waitTime = waitTimeInSeconds;  // 0 - no timeout
    }

    public void Add(T addIt)
    {
      _q.Enqueue(addIt);
      _available.Set();
    }

    public async Task<T> GetAsync()
    {
      T result = default(T);

      while (!_stop)
      {
        if (_q.TryDequeue(out result))
        {
          break;
        }
        else
        {
          CancellationTokenSource source = new CancellationTokenSource();
          if (_waitTime > 0)
          {
            source.CancelAfter(_waitTime * 1000);
          }

          CancellationToken token = source.Token;
          await _available.WaitAsync(token).ConfigureAwait(false);
          if (token.IsCancellationRequested)
          {
            break;
          }
        }
      }

      return result;
    }

    protected virtual void Dispose(bool disposing)
    {
      if (!disposedValue)
      {
        if (disposing)
        {
          _stop = true;
          _available.Set();
        }
        disposedValue = true;
      }
    }

    public void Dispose()
    {
      // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
      Dispose(disposing: true);
      GC.SuppressFinalize(this);
    }
  }
}
