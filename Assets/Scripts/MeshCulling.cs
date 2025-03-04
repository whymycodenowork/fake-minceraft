using UnityEngine;

public class MeshCulling : MonoBehaviour
{
    public Renderer objRenderer;

    private void Start()
    {
        objRenderer = GetComponent<Renderer>();
    }

    private void Update()
    {
        if (objRenderer != null)
        {
            objRenderer.enabled = GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(Camera.main), objRenderer.bounds);
        }
    }
}
