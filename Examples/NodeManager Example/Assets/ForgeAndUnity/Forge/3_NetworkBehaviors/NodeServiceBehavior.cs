using BeardedManStudios.Forge.Networking;
using BeardedManStudios.Forge.Networking.Unity;
using UnityEngine;

namespace BeardedManStudios.Forge.Networking.Generated
{
	[GeneratedRPC("{\"types\":[[\"uint\"][\"bool\", \"byte[]\"][\"bool\", \"byte[]\"][\"byte[]\"][\"byte[]\"][\"uint\", \"byte[]\"][\"byte[]\"][\"byte[]\"][\"byte[]\"][\"byte[]\"][\"byte[]\"][\"byte[]\"][\"byte[]\"][\"byte[]\"]]")]
	[GeneratedRPCVariableNames("{\"types\":[[\"nodeId\"][\"requireConfirmation\", \"data\"][\"requireConfirmation\", \"data\"][\"data\"][\"data\"][\"nodeId\", \"data\"][\"data\"][\"data\"][\"data\"][\"data\"][\"data\"][\"data\"][\"data\"][\"data\"]]")]
	public abstract partial class NodeServiceBehavior : NetworkBehavior
	{
		public const byte RPC_REGISTER_NODE = 0 + 5;
		public const byte RPC_REGISTER_SCENE = 1 + 5;
		public const byte RPC_UNREGISTER_SCENE = 2 + 5;
		public const byte RPC_CONFIRM_SCENE = 3 + 5;
		public const byte RPC_LOOKUP_SCENE = 4 + 5;
		public const byte RPC_RECEIVE_LOOKUP_SCENE = 5 + 5;
		public const byte RPC_RELAY_INSTANTIATE_IN_NODE = 6 + 5;
		public const byte RPC_INSTANTIATE_IN_NODE = 7 + 5;
		public const byte RPC_RELAY_CONFIRM_INSTANTIATE_IN_NODE = 8 + 5;
		public const byte RPC_CONFIRM_INSTANTIATE_IN_NODE = 9 + 5;
		public const byte RPC_RELAY_CREATE_NETWORK_SCENE_IN_NODE = 10 + 5;
		public const byte RPC_CREATE_NETWORK_SCENE_IN_NODE = 11 + 5;
		public const byte RPC_RELAY_CONFIRM_CREATE_NETWORK_SCENE_IN_NODE = 12 + 5;
		public const byte RPC_CONFIRM_CREATE_NETWORK_SCENE_IN_NODE = 13 + 5;
		
		public NodeServiceNetworkObject networkObject = null;

		public override void Initialize(NetworkObject obj)
		{
			// We have already initialized this object
			if (networkObject != null && networkObject.AttachedBehavior != null)
				return;
			
			networkObject = (NodeServiceNetworkObject)obj;
			networkObject.AttachedBehavior = this;

			base.SetupHelperRpcs(networkObject);
			networkObject.RegisterRpc("RegisterNode", RegisterNode, typeof(uint));
			networkObject.RegisterRpc("RegisterScene", RegisterScene, typeof(bool), typeof(byte[]));
			networkObject.RegisterRpc("UnregisterScene", UnregisterScene, typeof(bool), typeof(byte[]));
			networkObject.RegisterRpc("ConfirmScene", ConfirmScene, typeof(byte[]));
			networkObject.RegisterRpc("LookupScene", LookupScene, typeof(byte[]));
			networkObject.RegisterRpc("ReceiveLookupScene", ReceiveLookupScene, typeof(uint), typeof(byte[]));
			networkObject.RegisterRpc("RelayInstantiateInNode", RelayInstantiateInNode, typeof(byte[]));
			networkObject.RegisterRpc("InstantiateInNode", InstantiateInNode, typeof(byte[]));
			networkObject.RegisterRpc("RelayConfirmInstantiateInNode", RelayConfirmInstantiateInNode, typeof(byte[]));
			networkObject.RegisterRpc("ConfirmInstantiateInNode", ConfirmInstantiateInNode, typeof(byte[]));
			networkObject.RegisterRpc("RelayCreateNetworkSceneInNode", RelayCreateNetworkSceneInNode, typeof(byte[]));
			networkObject.RegisterRpc("CreateNetworkSceneInNode", CreateNetworkSceneInNode, typeof(byte[]));
			networkObject.RegisterRpc("RelayConfirmCreateNetworkSceneInNode", RelayConfirmCreateNetworkSceneInNode, typeof(byte[]));
			networkObject.RegisterRpc("ConfirmCreateNetworkSceneInNode", ConfirmCreateNetworkSceneInNode, typeof(byte[]));

			networkObject.onDestroy += DestroyGameObject;

			if (!obj.IsOwner)
			{
				if (!skipAttachIds.ContainsKey(obj.NetworkId)){
					uint newId = obj.NetworkId + 1;
					ProcessOthers(gameObject.transform, ref newId);
				}
				else
					skipAttachIds.Remove(obj.NetworkId);
			}

			if (obj.Metadata != null)
			{
				byte transformFlags = obj.Metadata[0];

				if (transformFlags != 0)
				{
					BMSByte metadataTransform = new BMSByte();
					metadataTransform.Clone(obj.Metadata);
					metadataTransform.MoveStartIndex(1);

					if ((transformFlags & 0x01) != 0 && (transformFlags & 0x02) != 0)
					{
						MainThreadManager.Run(() =>
						{
							transform.position = ObjectMapper.Instance.Map<Vector3>(metadataTransform);
							transform.rotation = ObjectMapper.Instance.Map<Quaternion>(metadataTransform);
						});
					}
					else if ((transformFlags & 0x01) != 0)
					{
						MainThreadManager.Run(() => { transform.position = ObjectMapper.Instance.Map<Vector3>(metadataTransform); });
					}
					else if ((transformFlags & 0x02) != 0)
					{
						MainThreadManager.Run(() => { transform.rotation = ObjectMapper.Instance.Map<Quaternion>(metadataTransform); });
					}
				}
			}

			MainThreadManager.Run(() =>
			{
				NetworkStart();
				networkObject.Networker.FlushCreateActions(networkObject);
			});
		}

		protected override void CompleteRegistration()
		{
			base.CompleteRegistration();
			networkObject.ReleaseCreateBuffer();
		}

		public override void Initialize(NetWorker networker, byte[] metadata = null)
		{
			Initialize(new NodeServiceNetworkObject(networker, createCode: TempAttachCode, metadata: metadata));
		}

		private void DestroyGameObject(NetWorker sender)
		{
			MainThreadManager.Run(() => { try { Destroy(gameObject); } catch { } });
			networkObject.onDestroy -= DestroyGameObject;
		}

		public override NetworkObject CreateNetworkObject(NetWorker networker, int createCode, byte[] metadata = null)
		{
			return new NodeServiceNetworkObject(networker, this, createCode, metadata);
		}

		protected override void InitializedTransform()
		{
			networkObject.SnapInterpolations();
		}

		/// <summary>
		/// Arguments:
		/// uint nodeId
		/// </summary>
		public abstract void RegisterNode(RpcArgs args);
		/// <summary>
		/// Arguments:
		/// bool requireConfirmation
		/// byte[] data
		/// </summary>
		public abstract void RegisterScene(RpcArgs args);
		/// <summary>
		/// Arguments:
		/// bool requireConfirmation
		/// byte[] data
		/// </summary>
		public abstract void UnregisterScene(RpcArgs args);
		/// <summary>
		/// Arguments:
		/// byte[] data
		/// </summary>
		public abstract void ConfirmScene(RpcArgs args);
		/// <summary>
		/// Arguments:
		/// byte[] data
		/// </summary>
		public abstract void LookupScene(RpcArgs args);
		/// <summary>
		/// Arguments:
		/// uint nodeId
		/// byte[] data
		/// </summary>
		public abstract void ReceiveLookupScene(RpcArgs args);
		/// <summary>
		/// Arguments:
		/// byte[] data
		/// </summary>
		public abstract void RelayInstantiateInNode(RpcArgs args);
		/// <summary>
		/// Arguments:
		/// byte[] data
		/// </summary>
		public abstract void InstantiateInNode(RpcArgs args);
		/// <summary>
		/// Arguments:
		/// byte[] data
		/// </summary>
		public abstract void RelayConfirmInstantiateInNode(RpcArgs args);
		/// <summary>
		/// Arguments:
		/// byte[] data
		/// </summary>
		public abstract void ConfirmInstantiateInNode(RpcArgs args);
		/// <summary>
		/// Arguments:
		/// byte[] data
		/// </summary>
		public abstract void RelayCreateNetworkSceneInNode(RpcArgs args);
		/// <summary>
		/// Arguments:
		/// byte[] data
		/// </summary>
		public abstract void CreateNetworkSceneInNode(RpcArgs args);
		/// <summary>
		/// Arguments:
		/// byte[] data
		/// </summary>
		public abstract void RelayConfirmCreateNetworkSceneInNode(RpcArgs args);
		/// <summary>
		/// Arguments:
		/// byte[] data
		/// </summary>
		public abstract void ConfirmCreateNetworkSceneInNode(RpcArgs args);

		// DO NOT TOUCH, THIS GETS GENERATED PLEASE EXTEND THIS CLASS IF YOU WISH TO HAVE CUSTOM CODE ADDITIONS
	}
}