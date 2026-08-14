using System;

namespace ThaleTheGreat.ClearMonocle.Types;

internal abstract class ClearMonocleObject : IDisposable {
    private IPurgeable? Purgeable => this as IPurgeable;

    internal ClearMonocleObject() {
        if (Purgeable is { } purgeable) {
            MemoryMonitor.Manager.Register(purgeable);
        }
    }

    ~ClearMonocleObject() {
        Debug.Trace($"{nameof(ClearMonocleObject)} ({this.GetType().FullName}) was not disposed before finalizer");
    }

    public virtual void Dispose() {
        GC.SuppressFinalize(this);

        if (Purgeable is { } purgeable) {
            MemoryMonitor.Manager.Unregister(purgeable);
        }
    }
}
