using System;
using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;

namespace KamiToolKit.Nodes;

public abstract unsafe partial class NodeBase : IDisposable {
    protected static readonly List<IDisposable> CreatedNodes = [];

    private bool isDisposed;
    protected Action<string> log;

    internal abstract AtkResNode* InternalResNode { get; }

    public static void DisposeAllNodes() {
        foreach (var node in CreatedNodes.ToArray()) {
            node.Dispose();
        }
    }

    ~NodeBase() => Dispose(false);

    public virtual void XDetach() {
        log.Invoke($"0x{(nint)InternalResNode:X} {InternalResNode->Type} XDetach()");
        log.Invoke("  RemoveTooltipEvents()");
        RemoveTooltipEvents();
        log.Invoke("  RemoveOnClickEvents()");
        RemoveOnClickEvents();
        log.Invoke("  DetachNode()");
        DetachNode();
    }

    protected virtual void Dispose(bool disposing) {
        log.Invoke($"0x{(nint)InternalResNode:X} {InternalResNode->Type} NodeBase:Dispose({disposing})");
        if (disposing) {
            // log.Invoke("  RemoveTooltipEvents()");
            // RemoveTooltipEvents();
            // log.Invoke("  RemoveOnClickEvents()");
            // RemoveOnClickEvents();
            // log.Invoke("  DetachNode()");
            // DetachNode();
        }
    }

    public void Dispose() {
        log.Invoke($"0x{(nint)InternalResNode:X} {InternalResNode->Type} NodeBase:Dispose [isDisposed={isDisposed}]");
        if (!isDisposed) {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        isDisposed = true;
    }
}

public abstract unsafe class NodeBase<T> : NodeBase where T : unmanaged, ICreatable {
    protected T* InternalNode { get; }

    internal override sealed AtkResNode* InternalResNode => (AtkResNode*) InternalNode;

    protected NodeBase(NodeType nodeType, Action<string> log)
    {
        this.log = log;
        InternalNode = NativeMemoryHelper.Create<T>();
        InternalResNode->Type = nodeType;

        if (InternalNode is null) {
            throw new Exception($"Unable to allocate memory for {typeof(T)}");
        }
        
        CreatedNodes.Add(this);
    }
    
    protected override void Dispose(bool disposing) {
        log.Invoke($"0x{(nint)InternalResNode:X} {InternalResNode->Type} NodeBase<T>:Dispose({disposing})");
        if (disposing) {
            base.Dispose(disposing);

            log.Invoke("  InternalResNode->Destroy(false)");
            InternalResNode->Destroy(false);
            log.Invoke("  NativeMemoryHelper.UiFree(InternalNode)");
            NativeMemoryHelper.UiFree(InternalNode);
        
            CreatedNodes.Remove(this);
        }
    }
}

