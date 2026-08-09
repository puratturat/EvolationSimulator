using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class CreatureVisualEvolution : MonoBehaviour
{
    private const string GeometryObjectName = "Evolution Geometry";

    private static Material sharedMaterial;

    private readonly List<Vector3> vertices = new List<Vector3>(192);
    private readonly List<int> triangles = new List<int>(384);
    private readonly List<Color> colors = new List<Color>(192);

    private CreatureStats stats;
    private SpriteRenderer bodyRenderer;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh visualMesh;

    public void Initialize(CreatureStats owner)
    {
        stats = owner;
        bodyRenderer = GetComponent<SpriteRenderer>();
        DisableLegacyMouth();
        EnsureGeometryObject();
        RefreshVisuals();
    }

    public void RefreshVisuals()
    {
        if (stats == null)
        {
            stats = GetComponent<CreatureStats>();
        }

        if (stats == null || stats.dna == null)
        {
            return;
        }

        stats.dna.UpdateSkinColorFromEcology();

        if (bodyRenderer == null)
        {
            bodyRenderer = GetComponent<SpriteRenderer>();
        }

        if (bodyRenderer != null)
        {
            bodyRenderer.color = CreateIndividualTint(stats.dna.skinColor);
        }

        EnsureGeometryObject();
        BuildGeometry();
    }

    private void DisableLegacyMouth()
    {
        Transform legacyMouth = transform.Find("Mouth");
        if (legacyMouth != null)
        {
            legacyMouth.gameObject.SetActive(false);
        }
    }

    private void EnsureGeometryObject()
    {
        if (visualMesh != null && meshFilter != null && meshRenderer != null)
        {
            return;
        }

        Transform existing = transform.Find(GeometryObjectName);
        GameObject geometryObject;
        if (existing != null)
        {
            geometryObject = existing.gameObject;
        }
        else
        {
            geometryObject = new GameObject(GeometryObjectName);
            geometryObject.transform.SetParent(transform, false);
        }

        geometryObject.transform.localPosition = new Vector3(0f, 0f, -0.01f);
        geometryObject.transform.localRotation = Quaternion.identity;
        geometryObject.transform.localScale = Vector3.one;

        meshFilter = geometryObject.GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            meshFilter = geometryObject.AddComponent<MeshFilter>();
        }

        meshRenderer = geometryObject.GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            meshRenderer = geometryObject.AddComponent<MeshRenderer>();
        }

        meshRenderer.sharedMaterial = GetSharedMaterial();
        if (bodyRenderer != null)
        {
            meshRenderer.sortingLayerID = bodyRenderer.sortingLayerID;
            meshRenderer.sortingOrder = bodyRenderer.sortingOrder + 2;
        }

        visualMesh = new Mesh
        {
            name = "Creature Evolution Geometry"
        };
        visualMesh.MarkDynamic();
        meshFilter.sharedMesh = visualMesh;
    }

    private static Material GetSharedMaterial()
    {
        if (sharedMaterial != null)
        {
            return sharedMaterial;
        }

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        }

        sharedMaterial = new Material(shader)
        {
            name = "Creature Geometry Material",
            hideFlags = HideFlags.HideAndDontSave
        };
        return sharedMaterial;
    }

    private void BuildGeometry()
    {
        vertices.Clear();
        triangles.Clear();
        colors.Clear();

        CreatureData dna = stats.dna;
        float plantScore = Mathf.Sqrt(Mathf.Clamp01(dna.desirePlant) * Mathf.Clamp01(dna.plantEfficiency * 0.5f));
        float meatScore = Mathf.Sqrt(Mathf.Clamp01(dna.desireMeat) * Mathf.Clamp01(dna.meatEfficiency * 0.5f));
        float poisonScore = Mathf.Sqrt(Mathf.Clamp01(dna.desirePoison) * Mathf.Clamp01(dna.poisonResistance));
        float totalDiet = Mathf.Max(0.001f, plantScore + meatScore + poisonScore);

        float plantShare = plantScore / totalDiet;
        float meatShare = meatScore / totalDiet;
        float poisonShare = poisonScore / totalDiet;
        float meatMorph = SmoothRange(meatScore, 0.04f, 0.62f);
        float poisonMorph = SmoothRange(poisonScore, 0.03f, 0.56f);

        float attackStrength = Mathf.InverseLerp(5f, 50f, dna.attackDamageMultiplier);
        float attackReach = Mathf.InverseLerp(0.5f, 4f, dna.attackDistance);
        float predatorMorph = meatMorph * Mathf.Sqrt(Mathf.Clamp01(attackStrength * attackReach));
        float speedMorph = Mathf.InverseLerp(0.5f, 10f, dna.moveSpeed);

        Color bodyColor = bodyRenderer != null ? bodyRenderer.color : dna.skinColor;
        Color darkBody = Color.Lerp(bodyColor, Color.black, 0.48f);
        Color lightBody = Color.Lerp(bodyColor, Color.white, 0.62f);
        Color poisonAccent = Color.Lerp(new Color(0.72f, 0.16f, 0.95f, 1f), Color.white, 0.18f);

        AddSpeedTail(speedMorph, darkBody, dna.patternSeed);
        AddPredatorSpikes(predatorMorph, darkBody, dna.patternSeed);
        AddPoisonSpots(poisonMorph, poisonAccent, dna.patternSeed);
        AddPlantMouth(plantShare, plantScore, lightBody);
        AddMeatMouth(meatShare, meatMorph, darkBody);
        AddPoisonMouth(poisonShare, poisonMorph, poisonAccent);

        visualMesh.Clear();
        visualMesh.SetVertices(vertices);
        visualMesh.SetTriangles(triangles, 0, true);
        visualMesh.SetColors(colors);
        visualMesh.RecalculateBounds();
    }

    private void AddSpeedTail(float speedMorph, Color color, float seed)
    {
        float length = Mathf.Lerp(0.10f, 1.20f, Mathf.Pow(speedMorph, 0.72f));
        float width = Mathf.Lerp(0.018f, 0.085f, speedMorph);
        int segments = 5;
        Vector2 previous = new Vector2(0f, -0.43f);
        float phase = seed * Mathf.PI * 2f;

        for (int index = 1; index <= segments; index++)
        {
            float progress = index / (float)segments;
            float wave = Mathf.Sin((progress * Mathf.PI * 2.2f) + phase) * length * 0.13f * progress;
            Vector2 current = new Vector2(wave, -0.43f - (length * progress));
            AddThickSegment(previous, current, width * Mathf.Lerp(1f, 0.28f, progress), color);
            previous = current;
        }
    }

    private void AddPredatorSpikes(float morph, Color color, float seed)
    {
        if (morph <= 0.035f)
        {
            return;
        }

        int count = 2 + Mathf.RoundToInt(morph * 8f);
        float height = Mathf.Lerp(0.035f, 0.25f, morph);
        for (int index = 0; index < count; index++)
        {
            float t = count == 1 ? 0.5f : index / (float)(count - 1);
            float angle = Mathf.Lerp(48f, 312f, t) + Mathf.Lerp(-8f, 8f, Hash01(seed, index + 31));
            float radians = angle * Mathf.Deg2Rad;
            Vector2 outward = new Vector2(Mathf.Sin(radians), Mathf.Cos(radians));
            Vector2 tangent = new Vector2(-outward.y, outward.x);
            Vector2 center = new Vector2(outward.x * 0.43f, outward.y * 0.48f);
            float halfBase = Mathf.Lerp(0.025f, 0.075f, morph);
            AddTriangle(center - (tangent * halfBase), center + (tangent * halfBase), center + (outward * height), color);
        }
    }

    private void AddPoisonSpots(float morph, Color color, float seed)
    {
        if (morph <= 0.035f)
        {
            return;
        }

        int count = 2 + Mathf.RoundToInt(morph * 10f);
        Color spotColor = color;
        spotColor.a = Mathf.Lerp(0.55f, 0.95f, morph);

        for (int index = 0; index < count; index++)
        {
            float angle = Hash01(seed, index * 3 + 101) * Mathf.PI * 2f;
            float radius = Mathf.Sqrt(Hash01(seed, index * 3 + 102));
            Vector2 position = new Vector2(
                Mathf.Cos(angle) * radius * 0.32f,
                Mathf.Sin(angle) * radius * 0.37f);
            float size = Mathf.Lerp(0.018f, 0.065f, morph) * Mathf.Lerp(0.72f, 1.28f, Hash01(seed, index * 3 + 103));
            AddPolygon(position, size, 7, spotColor, angle);
        }
    }

    private void AddPlantMouth(float share, float score, Color color)
    {
        if (share <= 0.035f)
        {
            return;
        }

        float length = Mathf.Lerp(0.05f, 0.38f, share) * Mathf.Lerp(0.7f, 1.15f, score);
        float width = Mathf.Lerp(0.018f, 0.065f, share);
        Vector2 start = new Vector2(0f, 0.40f);
        Vector2 end = new Vector2(0f, 0.40f + length);
        Color mouthColor = color;
        mouthColor.a = Mathf.Lerp(0.4f, 1f, share);
        AddThickSegment(start, end, width, mouthColor);
        AddPolygon(end, width * 0.9f, 8, mouthColor, 0f);
    }

    private void AddMeatMouth(float share, float morph, Color color)
    {
        if (morph <= 0.025f)
        {
            return;
        }

        int toothCount = 2 + Mathf.RoundToInt(share * 5f);
        float spread = Mathf.Lerp(0.09f, 0.34f, morph);
        float toothLength = Mathf.Lerp(0.035f, 0.22f, morph);
        float halfWidth = Mathf.Lerp(0.018f, 0.055f, morph);
        Color toothColor = Color.Lerp(new Color(1f, 0.91f, 0.66f, 1f), color, 0.16f);

        for (int index = 0; index < toothCount; index++)
        {
            float t = toothCount == 1 ? 0.5f : index / (float)(toothCount - 1);
            float x = Mathf.Lerp(-spread, spread, t);
            float baseY = 0.43f - (Mathf.Abs(t - 0.5f) * 0.05f);
            AddTriangle(
                new Vector2(x - halfWidth, baseY),
                new Vector2(x + halfWidth, baseY),
                new Vector2(x, baseY + toothLength),
                toothColor);
        }
    }

    private void AddPoisonMouth(float share, float morph, Color color)
    {
        if (morph <= 0.025f)
        {
            return;
        }

        int filamentCount = 3 + Mathf.RoundToInt(share * 7f);
        float length = Mathf.Lerp(0.06f, 0.34f, morph);
        float width = Mathf.Lerp(0.008f, 0.024f, morph);

        for (int index = 0; index < filamentCount; index++)
        {
            float t = filamentCount == 1 ? 0.5f : index / (float)(filamentCount - 1);
            float angle = Mathf.Lerp(-58f, 58f, t) * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
            Vector2 start = new Vector2(Mathf.Lerp(-0.14f, 0.14f, t), 0.42f);
            Vector2 end = start + (direction * length * Mathf.Lerp(0.75f, 1.08f, 1f - Mathf.Abs(t - 0.5f) * 2f));
            AddThickSegment(start, end, width, color);
            AddPolygon(end, width * 1.35f, 6, color, angle);
        }
    }

    private Color CreateIndividualTint(Color ecologicalColor)
    {
        Color.RGBToHSV(ecologicalColor, out float hue, out float saturation, out float value);
        float identity = Hash01(stats.dna.patternSeed, (int)(stats.observationId % 997L) + (stats.generation * 17));
        hue = Mathf.Repeat(hue + Mathf.Lerp(-0.028f, 0.028f, identity), 1f);
        value = Mathf.Clamp01(value + Mathf.Lerp(-0.07f, 0.07f, Hash01(stats.dna.lineageHue, stats.generation + 401)));
        return Color.HSVToRGB(hue, Mathf.Clamp01(saturation), value);
    }

    private void AddThickSegment(Vector2 start, Vector2 end, float width, Color color)
    {
        Vector2 direction = end - start;
        if (direction.sqrMagnitude <= 0.000001f)
        {
            return;
        }

        Vector2 normal = new Vector2(-direction.y, direction.x).normalized * width;
        int first = vertices.Count;
        AddVertex(start - normal, color);
        AddVertex(start + normal, color);
        AddVertex(end + normal, color);
        AddVertex(end - normal, color);
        AddQuadTriangles(first);
    }

    private void AddTriangle(Vector2 a, Vector2 b, Vector2 c, Color color)
    {
        int first = vertices.Count;
        AddVertex(a, color);
        AddVertex(b, color);
        AddVertex(c, color);
        triangles.Add(first);
        triangles.Add(first + 1);
        triangles.Add(first + 2);
    }

    private void AddPolygon(Vector2 center, float radius, int sides, Color color, float rotation)
    {
        int centerIndex = vertices.Count;
        AddVertex(center, color);
        for (int index = 0; index < sides; index++)
        {
            float angle = rotation + (index / (float)sides * Mathf.PI * 2f);
            AddVertex(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, color);
        }

        for (int index = 0; index < sides; index++)
        {
            triangles.Add(centerIndex);
            triangles.Add(centerIndex + 1 + index);
            triangles.Add(centerIndex + 1 + ((index + 1) % sides));
        }
    }

    private void AddVertex(Vector2 position, Color color)
    {
        vertices.Add(new Vector3(position.x, position.y, 0f));
        colors.Add(color);
    }

    private void AddQuadTriangles(int first)
    {
        triangles.Add(first);
        triangles.Add(first + 1);
        triangles.Add(first + 2);
        triangles.Add(first);
        triangles.Add(first + 2);
        triangles.Add(first + 3);
    }

    private static float SmoothRange(float value, float minimum, float maximum)
    {
        float normalized = Mathf.InverseLerp(minimum, maximum, value);
        return normalized * normalized * (3f - (2f * normalized));
    }

    private static float Hash01(float seed, int salt)
    {
        float value = Mathf.Sin((seed * 127.1f) + (salt * 311.7f)) * 43758.5453f;
        return Mathf.Repeat(value, 1f);
    }

    private void OnDestroy()
    {
        if (visualMesh != null)
        {
            Destroy(visualMesh);
        }
    }
}
