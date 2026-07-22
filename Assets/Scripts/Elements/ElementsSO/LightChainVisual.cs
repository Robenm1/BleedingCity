using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(LineRenderer))]
public class LightChainVisual : MonoBehaviour
{
    [Header("Lifetime")]
    public float lifetime = 0.12f;

    [Tooltip("How often the lightning shape changes while alive.")]
    public float flickerInterval = 0.02f;

    [Header("Shape")]
    [Tooltip("More segments = more jagged lightning detail.")]
    [Min(2)] public int segments = 10;

    [Tooltip("How far the lightning bends away from the straight line.")]
    public float jaggedAmount = 0.25f;

    [Tooltip("Adds small forward/backward distortion along the line.")]
    public float alongLineJitter = 0.08f;

    [Header("Width")]
    public float startWidth = 0.1f;
    public float endWidth = 0.03f;

    [Tooltip("Random width flicker amount.")]
    public float widthFlicker = 0.04f;

    [Header("Branches")]
    public bool createBranches = true;

    [Range(0f, 1f)]
    public float branchChancePerFlicker = 0.6f;

    [Min(0)] public int maxBranches = 2;
    public float branchLength = 0.5f;
    public float branchWidthMultiplier = 0.45f;

    [Header("Z Offset")]
    public float zOffset = -1f;

    private LineRenderer _mainLine;
    private LineRenderer[] _branchLines;

    private Coroutine _routine;

    private Vector3 _start;
    private Vector3 _end;
    private Color _baseColor = Color.white;

    private void Awake()
    {
        _mainLine = GetComponent<LineRenderer>();
        SetupMainLine();
        SetupBranches();
    }

    private void SetupMainLine()
    {
        if (_mainLine == null)
            _mainLine = GetComponent<LineRenderer>();

        _mainLine.useWorldSpace = true;
        _mainLine.positionCount = segments + 1;
        _mainLine.startWidth = startWidth;
        _mainLine.endWidth = endWidth;
        _mainLine.numCapVertices = 4;
        _mainLine.numCornerVertices = 4;
    }

    private void SetupBranches()
    {
        if (!createBranches || maxBranches <= 0)
            return;

        _branchLines = new LineRenderer[maxBranches];

        for (int i = 0; i < maxBranches; i++)
        {
            GameObject branchObj = new GameObject($"LightningBranch_{i}");
            branchObj.transform.SetParent(transform);

            LineRenderer branch = branchObj.AddComponent<LineRenderer>();

            branch.useWorldSpace = true;
            branch.positionCount = 2;
            branch.startWidth = startWidth * branchWidthMultiplier;
            branch.endWidth = 0f;
            branch.numCapVertices = 3;
            branch.numCornerVertices = 3;
            branch.enabled = false;

            if (_mainLine != null && _mainLine.sharedMaterial != null)
                branch.sharedMaterial = _mainLine.sharedMaterial;

            branch.sortingLayerID = _mainLine.sortingLayerID;
            branch.sortingOrder = _mainLine.sortingOrder + 1;

            _branchLines[i] = branch;
        }
    }

    public void Play(Vector3 start, Vector3 end, Color color)
    {
        if (_mainLine == null)
            SetupMainLine();

        if (_branchLines == null || _branchLines.Length != maxBranches)
            SetupBranches();

        _start = start;
        _end = end;
        _baseColor = color;

        _start.z = zOffset;
        _end.z = zOffset;

        if (_routine != null)
            StopCoroutine(_routine);

        _routine = StartCoroutine(LightningRoutine());
    }

    private IEnumerator LightningRoutine()
    {
        float timer = 0f;
        float flickerTimer = 0f;

        GenerateLightningShape(1f);

        while (timer < lifetime)
        {
            timer += Time.deltaTime;
            flickerTimer += Time.deltaTime;

            float normalizedLife = lifetime > 0f ? timer / lifetime : 1f;
            float alpha = Mathf.Lerp(1f, 0f, normalizedLife);

            ApplyAlpha(alpha);

            if (flickerTimer >= flickerInterval)
            {
                flickerTimer = 0f;
                GenerateLightningShape(alpha);
            }

            yield return null;
        }

        Destroy(gameObject);
    }

    private void GenerateLightningShape(float alpha)
    {
        if (_mainLine == null)
            return;

        segments = Mathf.Max(2, segments);

        _mainLine.positionCount = segments + 1;

        Vector3 direction = _end - _start;

        if (direction.sqrMagnitude <= 0.0001f)
            direction = Vector3.right;

        Vector3 dirNormal = direction.normalized;
        Vector3 perpendicular = Vector3.Cross(dirNormal, Vector3.forward).normalized;

        float randomWidth = Random.Range(-widthFlicker, widthFlicker);
        _mainLine.startWidth = Mathf.Max(0.01f, startWidth + randomWidth);
        _mainLine.endWidth = Mathf.Max(0.005f, endWidth + randomWidth * 0.5f);

        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            Vector3 point = Vector3.Lerp(_start, _end, t);

            if (i != 0 && i != segments)
            {
                float centerFade = Mathf.Sin(t * Mathf.PI);

                float sideOffset = Random.Range(-jaggedAmount, jaggedAmount) * centerFade;
                float forwardOffset = Random.Range(-alongLineJitter, alongLineJitter) * centerFade;

                point += perpendicular * sideOffset;
                point += dirNormal * forwardOffset;
            }

            point.z = zOffset;
            _mainLine.SetPosition(i, point);
        }

        _mainLine.startColor = WithAlpha(_baseColor, alpha);
        _mainLine.endColor = WithAlpha(_baseColor, alpha * 0.7f);

        GenerateBranches(perpendicular, alpha);
    }

    private void GenerateBranches(Vector3 perpendicular, float alpha)
    {
        if (!createBranches || _branchLines == null)
            return;

        for (int i = 0; i < _branchLines.Length; i++)
        {
            LineRenderer branch = _branchLines[i];

            if (branch == null)
                continue;

            bool shouldShow = Random.value <= branchChancePerFlicker;

            if (!shouldShow)
            {
                branch.enabled = false;
                continue;
            }

            branch.enabled = true;

            int startIndex = Random.Range(1, Mathf.Max(2, _mainLine.positionCount - 1));
            Vector3 branchStart = _mainLine.GetPosition(startIndex);

            float side = Random.value > 0.5f ? 1f : -1f;
            Vector3 branchDir = perpendicular * side;

            branchDir += Random.insideUnitSphere * 0.25f;
            branchDir.z = 0f;
            branchDir.Normalize();

            Vector3 branchEnd = branchStart + branchDir * Random.Range(branchLength * 0.5f, branchLength);
            branchEnd.z = zOffset;

            branch.SetPosition(0, branchStart);
            branch.SetPosition(1, branchEnd);

            branch.startWidth = startWidth * branchWidthMultiplier * Random.Range(0.7f, 1.2f);
            branch.endWidth = 0f;

            branch.startColor = WithAlpha(_baseColor, alpha * 0.8f);
            branch.endColor = WithAlpha(_baseColor, 0f);
        }
    }

    private void ApplyAlpha(float alpha)
    {
        if (_mainLine != null)
        {
            _mainLine.startColor = WithAlpha(_baseColor, alpha);
            _mainLine.endColor = WithAlpha(_baseColor, alpha * 0.7f);
        }

        if (_branchLines == null)
            return;

        for (int i = 0; i < _branchLines.Length; i++)
        {
            LineRenderer branch = _branchLines[i];

            if (branch == null || !branch.enabled)
                continue;

            branch.startColor = WithAlpha(_baseColor, alpha * 0.8f);
            branch.endColor = WithAlpha(_baseColor, 0f);
        }
    }

    private Color WithAlpha(Color color, float alpha)
    {
        color.a = Mathf.Clamp01(alpha);
        return color;
    }
}