using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class LightProbeRuntime : MonoBehaviour
{
    
    #region -- 資源參考區 --

    public Color m_Ambient;
    Light[] m_Lights;

    #endregion

    #region -- 初始化/運作 --

    IEnumerator Start()
    {
        yield return null;

        m_Lights = FindObjectsByType<Light>(FindObjectsSortMode.InstanceID);
        SphericalHarmonicsL2[] bakedProbes = LightmapSettings.lightProbes.bakedProbes;
        Vector3[] probePositions = LightmapSettings.lightProbes.positions;
        int probeCount = LightmapSettings.lightProbes.count;

        // Clear all probes
        for (int i = 0; i < probeCount; i++)
            bakedProbes[i].Clear();

        // Add ambient light to all probes
        for (int i = 0; i < probeCount; i++)
            bakedProbes[i].AddAmbientLight(m_Ambient);

        // Add directional and point lights' contribution to all probes
        foreach (Light l in m_Lights)
        {
            if (l.type == LightType.Directional)
            {
                for (int i = 0; i < probeCount; i++)
                    bakedProbes[i].AddDirectionalLight(-l.transform.forward, l.color, l.intensity);
            }
            else if (l.type == LightType.Point)
            {
                for (int i = 0; i < probeCount; i++)
                    SHAddPointLight(probePositions[i], l.transform.position, l.range, l.color, l.intensity, ref bakedProbes[i]);
            }
        }
        LightmapSettings.lightProbes.bakedProbes = bakedProbes;
    }

    #endregion

    #region -- 方法參考區 --

    void SHAddPointLight(Vector3 probePosition, Vector3 position, float range, Color color, float intensity, ref SphericalHarmonicsL2 sh)
    {
        // From the point of view of an SH probe, point light looks no different than a directional light,
        // just attenuated and coming from the right direction.
        Vector3 probeToLight = position - probePosition;
        float attenuation = 1.0F / (1.0F + 25.0F * probeToLight.sqrMagnitude / (range * range));
        sh.AddDirectionalLight(probeToLight.normalized, color, intensity * attenuation);
    }

    #endregion

}