using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.XR.Templates.AR
{
    public class ObjectMaterialChanger : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("La lista de materiales (filtros) disponibles para aplicar.")]
        List<Material> m_MaterialFilters = new List<Material>();

        public List<Material> materialFilters
        {
            get => m_MaterialFilters;
            set => m_MaterialFilters = value;
        }

        public void ApplyMaterial(GameObject targetObject, int materialIndex)
        {
            // Validamos que el objeto y el índice sean correctos
            if (targetObject == null) return;
            if (materialIndex < 0 || materialIndex >= m_MaterialFilters.Count)
            {
                Debug.LogWarning("Índice de material fuera de rango.");
                return;
            }

            // Buscamos el componente Renderer en el objeto (o en sus hijos)
            Renderer objectRenderer = targetObject.GetComponentInChildren<Renderer>();

            if (objectRenderer != null)
            {
                // Aplicamos el nuevo material
                objectRenderer.material = m_MaterialFilters[materialIndex];
                Debug.Log("Material cambiado a "+ materialIndex);
            }
            else
            {
                Debug.LogWarning("No se encontró un Renderer en el objeto seleccionado.", targetObject);
            }
        }
    }
}