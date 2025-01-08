using UnityEngine;

public class BackgroundMusicTrigger : MonoBehaviour
{

    #region -- 資源參考區 --

    public BackgroundMusicSO.BackgroundMusicType backgroundMusicType;

    #endregion

    #region -- 初始化/運作 --

    private void OnTriggerEnter(Collider other)
    {

        if (other.tag == "Player")
        {

            GameManagerSingleton.Instance.BackgroundMusicSystem.BGMSelect(backgroundMusicType);

        }

    }

    #endregion

}