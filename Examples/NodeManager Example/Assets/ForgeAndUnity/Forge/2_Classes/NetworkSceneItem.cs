/// <summary>
/// Used by the <see cref="NodeManager"/> relate a scene to a <see cref="NetworkSceneManager"/>.
/// Exposes events describing the milestones during scene-creation like <see cref="OnCreated"/> or <see cref="OnReady"/>.
/// </summary>
public class NetworkSceneItem {
    //Fields
    protected NetworkSceneTemplate          _sceneTemplate;
    protected NetworkSceneManager           _manager;
    protected bool                          _isCreated;
    protected bool                          _isRegistered;
    protected bool                          _isUnregistered;
    protected bool                          _isReady;

    public NetworkSceneTemplate             SceneTemplate       { get { return _sceneTemplate; } set { _sceneTemplate = value; } }
    public NetworkSceneManager              Manager             { get { return _manager; } set { _manager = value; } }
    public bool                             HasManager          { get { return _manager != null; } }
    public bool                             IsCreated           { get { return _isCreated; } set { _isCreated = value; } }
    public bool                             IsRegistered        { get { return _isRegistered; } set { _isRegistered = value; } }
    public bool                             IsUnregistered      { get { return _isUnregistered; } set { _isUnregistered = value; } }
    public bool                             IsReady             { get { return _isReady; } set { _isReady = value; } }

    //Events
    public delegate void CreatedEvent (NetworkSceneItem pItem);
    public event CreatedEvent OnCreated;
    public delegate void RegisteredEvent (NetworkSceneItem pItem);
    public event RegisteredEvent OnRegistered;
    public delegate void UnregisteredEvent (NetworkSceneItem pItem);
    public event UnregisteredEvent OnUnregistered;
    public delegate void ReadyEvent (NetworkSceneItem pItem);
    public event ReadyEvent OnReady;
    public delegate void UnloadedEvent (NetworkSceneItem pItem);
    public event UnloadedEvent OnUnloaded;


    //Functions
    public NetworkSceneItem () { }

    public NetworkSceneItem (NetworkSceneTemplate pNetworkSceneTemplate, NetworkSceneManager pNetworkSceneManager) {
        _sceneTemplate = pNetworkSceneTemplate;
        _manager = pNetworkSceneManager;
    }

    public virtual void RaiseCreated () {
        if (OnCreated != null) {
            OnCreated(this);
        }
    }

    public virtual void RaiseRegistered () {
        if (OnRegistered != null) {
            OnRegistered(this);
        }
    }

    public virtual void RaiseUnregistered () {
        if (OnUnregistered != null) {
            OnUnregistered(this);
        }
    }

    public virtual void RaiseReady() {
        if (OnReady != null) {
            OnReady(this);
        }
    }

    public virtual void RaiseUnloaded () {
        if (OnUnloaded != null) {
            OnUnloaded(this);
        }
    }

    public virtual RPCNetworkSceneItem ToRPC () {
        return new RPCNetworkSceneItem() {
            sceneTemplate = _sceneTemplate.ToRPC()
            };
    }
}
