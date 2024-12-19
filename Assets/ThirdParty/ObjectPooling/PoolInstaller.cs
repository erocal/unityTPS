using UnityEngine;

namespace ToolBox.Pools
{
    [DefaultExecutionOrder(-9999), DisallowMultipleComponent]
    public sealed class PoolInstaller : MonoBehaviour
    {

        #region -- 資源參考區 --

        [SerializeField] private PoolContainerSO poolContainerSO;

        #endregion

        #region -- 變數參考區 --

        private const string SO_ADDRESS = "poolcontainerso";

        #endregion

        #region -- 初始化/運作 --

        private async void Awake()
        {

            if (poolContainerSO == null)
            {

                var handle = await AddrssableAsync.LoadAsync<PoolContainerSO>(SO_ADDRESS);

                if (handle.IsValid())
                {
                    poolContainerSO = handle.Result;
                }

            }

            var _pools = poolContainerSO._pools;


            for (var i = 0; i < _pools.Length; i++)
                _pools[i].Populate(this.transform);

        }

        #endregion

        [System.Serializable]
        private struct PoolContainer
        {
            [SerializeField] private GameObject _prefab;
            [SerializeField, Min(1)] private int _startCount;

            public void Populate()
            {
                _prefab.Populate(_startCount);
            }

            public void Populate(Transform transform)
            {
                _prefab.Populate(transform, _startCount);
            }

        }
    }
}
