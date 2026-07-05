using UnityEngine;

public class AbyssalDollWaveVisual : MonoBehaviour
{
    private AbyssalDollObject _doll;

    private void Awake()
    {
        _doll = GetComponentInParent<AbyssalDollObject>();
    }

    public void HideWaveVisual()
    {
        if (_doll != null)
            _doll.HideWaveVisual();
        else
            gameObject.SetActive(false);
    }
}