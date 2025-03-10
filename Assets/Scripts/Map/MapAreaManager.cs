﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class MapAreaManager : MonoBehaviour
{

    #region -- 變數參考區 --

    private ActionSystem actionSystem;

    private Dictionary<int, GameObject> mapAreaDictionary = new Dictionary<int, GameObject>()
    {
        { 0, null},
        { 1, null},
        { 2, null},
        { 3, null},
        { 4, null},
    };

    private Dictionary<int, List<GameObject>> areaEnemyDictionary = new Dictionary<int, List<GameObject>>();

    #endregion

    #region -- 初始化/運作 --

    private void Awake()
    {
        
        actionSystem = GameManagerSingleton.Instance.ActionSystem;

        SetAreaEnemyDictionary();

        actionSystem.OnMapAreaSwitch += SwitchMapArea;

    }

    #endregion

    #region -- 方法參考區 --

    public async void SetAreaEnemyDictionary()
    {

        // 等待直到 organism 不為 null
        while (GameManagerSingleton.Instance.Organism == null)
        {
            await Task.Delay(3000);
        }

        // 當 organism 不為 null 時，執行後續程式碼
        var organism = GameManagerSingleton.Instance.Organism;

        areaEnemyDictionary.Add((int)MapAreaType.StartArea, organism.StartAreaEnemyList);
        areaEnemyDictionary.Add((int)MapAreaType.PlainArea, organism.PlainAreaEnemyList);
        areaEnemyDictionary.Add((int)MapAreaType.PlagueDoctorArea, organism.PlagueDoctorAreaEnemyList);
        areaEnemyDictionary.Add((int)MapAreaType.MutantArea, organism.MutantAreaEnemyList);

    }

    /// <summary>
    /// 切換地圖區域
    /// </summary>
    /// <param name="mapAreaTriggerid">區域id</param>
    /// /// <param name="enemyList">區域存在敵人</param>
    public async Task SwitchMapArea(int mapAreaTriggerid)
    {

        switch (mapAreaTriggerid)
        {
            case 0:
                await LoadMapAreaAsync((int)MapAreaType.StartArea, MapAreaType.StartArea);
                await LoadMapAreaAsync((int)MapAreaType.PlainArea, MapAreaType.PlainArea);
                UnloadMapArea(MapAreaType.PlagueDoctorArea);
                UnloadMapArea(MapAreaType.MutantArea);
                break;
            case 1:
            case 2:
                UnloadMapArea(MapAreaType.StartArea);
                await LoadMapAreaAsync((int)MapAreaType.PlainArea, MapAreaType.PlainArea);
                await LoadMapAreaAsync((int)MapAreaType.PlagueDoctorArea, MapAreaType.PlagueDoctorArea);
                UnloadMapArea(MapAreaType.MutantArea);
                break;
            case 3:
                UnloadMapArea(MapAreaType.StartArea);
                await LoadMapAreaAsync((int)MapAreaType.PlainArea, MapAreaType.PlainArea);
                UnloadMapArea(MapAreaType.PlagueDoctorArea);
                await LoadMapAreaAsync((int)MapAreaType.MutantArea, MapAreaType.MutantArea);
                break;
        }

        ShowEnemy(mapAreaTriggerid, areaEnemyDictionary[mapAreaTriggerid]);

    }

    private void ShowEnemy(int mapAreaTriggerid, List<GameObject> enemyList = null)
    {

        var organism = GameManagerSingleton.Instance.Organism;

        if (organism.fullEnemyList.Count < 1)
        {
            Log.Error("場景全敵人List為空!");
            return;
        }
        else if (enemyList.Count < 1)
        {
            Log.Error("區域敵人List為空!");
            return;
        }

        var enemyCompareResultList =  GameObjectComparerHelper.CompareAndExecuteMethods(organism.fullEnemyList, enemyList);
        SetActiveEnemy(enemyCompareResultList.commonElementsList, true);
        SetActiveEnemy(enemyCompareResultList.differentElementsList, false);


    }

    /// <summary>
    /// 載入地圖區域
    /// </summary>
    /// <param name="index">地圖區域編號</param>
    /// <param name="mapAreaType">地圖區域</param>
    private async Task LoadMapAreaAsync(int index, MapAreaType mapAreaType)
    {
        try
        {
            if (mapAreaDictionary[index] == null)
            {

                var mapAreaGameObject = await AddrssableAsync.LoadInstantiate(mapAreaType.ToString().ToLower(), this.transform);

                // 檢查操作是否成功
                if (mapAreaGameObject)
                {
                    mapAreaDictionary[index] = mapAreaGameObject;
                }
                else
                {
                    // 處理載入失敗的情況
                    Log.Error($"無法載入或生成 asset: {mapAreaGameObject}");
                }
            }
        }
        catch (Exception ex)
        {
            // 處理異常情況
            Log.Error($"錯誤: {ex.Message}");
        }
    }

    /// <summary>
    /// 卸載地圖區域
    /// </summary>
    /// <param name="index">地圖區域編號</param>
    /// <param name="mapAreaType">地圖區域</param>
    private void UnloadMapArea(MapAreaType mapAreaType)
    {
        if (mapAreaDictionary[(int)mapAreaType] != null)
        {
            AddrssableAsync.Unload(mapAreaDictionary[(int)mapAreaType]);
            mapAreaDictionary[(int)mapAreaType] = null;
        }
    }

    /// <summary>
    /// 開啟/關閉敵人
    /// </summary>
    /// <param name="enemyList">儲存敵人物件的List</param>
    /// <param name="isActive">是否開啟</param>
    public void SetActiveEnemy(List<GameObject> enemyList, bool isActive)
    {
        if (enemyList.Count > 0)
        {
            foreach (var enemyGameObject in enemyList)
            {
                enemyGameObject.SetActive(isActive);
            }
        }
    }

    #endregion

}
