using UnityEngine;
using System.Collections;

public class AngularTreeScript : MonoBehaviour {

    private Vector3 tmp;
    private Vector2 tmp2;
    private Mesh mesh;
    private float topY, bottomY;

    public float angle;

    public GameObject crown, crownTop, crownBot, sphere;
    private int mode;
    private Material originalMaterial;

    void Start()
    {
        this.originalMaterial = crown.GetComponent<Renderer>().material;
    }

    public void ChangeTopRing(float f)
    {
        Mesh crownMesh = crown.GetComponent<MeshFilter>().mesh;
        Mesh crownTopMesh = crownTop.GetComponent<MeshFilter>().mesh;
        Mesh crownBotMesh = crownBot.GetComponent<MeshFilter>().mesh;
        Mesh sphereMesh = sphere.GetComponent<MeshFilter>().mesh;

        if (mode == 1)
        {
            mesh = crownMesh;
        }
        else if (mode == 2)
        {
            mesh = crownBotMesh;
        }
        else if (mode == 3)
        {
            mesh = sphereMesh;
        }

        Vector3[] vertices = mesh.vertices;

        //print(vertices.Length);
        topY = vertices[0].y;
        bottomY = vertices[1].y;
        int topCount = 0;
        int bottomCount = 0;

        // find top and bottom Y. Count vertices
        for (int i = 0; i < vertices.Length; i++)
        {
            //print(vertices[i]);
            if (vertices[i].y >= topY)
            {
                topY = vertices[i].y;
                topCount++;
            }
            if (vertices[i].y <= bottomY)
            {
                bottomY = vertices[i].y;
                bottomCount++;
            }
        }

        this.angle = f;

        // unity scales cylinder's y*2
        // assuming regular cylinders (x==z)
        float a = Mathf.Tan(Mathf.Deg2Rad * f) * ((2f * (this.transform.localScale.y / this.transform.localScale.x)) * Mathf.Abs(topY - bottomY));
        for (int i = 0; i < vertices.Length; i++)
        {
            if (vertices[i].y == topY && vertices[i].x != vertices[i].z)
            {
                tmp = vertices[i];
                tmp2 = new Vector2(tmp.x, tmp.y);
                tmp.x *= (a + tmp2.magnitude);
                tmp.z *= (a + tmp2.magnitude);
                vertices[i] = tmp;
            }
        }

        mesh.vertices = vertices;
        mesh.RecalculateBounds();
        vertices = null;
    }

    public void ChangeBottomRing(float f)
    {
        Mesh crownMesh = crown.GetComponent<MeshFilter>().mesh;
        Mesh crownTopMesh = crownTop.GetComponent<MeshFilter>().mesh;
        Mesh crownBotMesh = crownBot.GetComponent<MeshFilter>().mesh;
        Mesh sphereMesh = sphere.GetComponent<MeshFilter>().mesh;

        if (mode == 1)
        {
            mesh = crownMesh;
        }
        else if (mode == 2)
        {
            mesh = crownTopMesh;
        }
        else if (mode == 3)
        {
            mesh = sphereMesh;
        }

        Vector3[] vertices = mesh.vertices;

        //print(vertices.Length);
        topY = vertices[0].y;
        bottomY = vertices[1].y;
        int topCount = 0;
        int bottomCount = 0;

        // find top and bottom Y. Count vertices
        for (int i = 0; i < vertices.Length; i++)
        {
            //print(vertices[i]);
            if (vertices[i].y >= topY)
            {
                topY = vertices[i].y;
                topCount++;
            }
            if (vertices[i].y <= bottomY)
            {
                bottomY = vertices[i].y;
                bottomCount++;
            }
        }

        this.angle = f;

        // unity scales cylinder's y*2
        // assuming regular cylinders (x==z)
        float a = Mathf.Tan(Mathf.Deg2Rad * f) * ((2f * (this.transform.localScale.y / this.transform.localScale.x)) * Mathf.Abs(topY - bottomY));
        for (int i = 0; i < vertices.Length; i++)
        {
            if (vertices[i].y == bottomY && vertices[i].x != vertices[i].z)
            {
                tmp = vertices[i];
                tmp2 = new Vector2(tmp.x, tmp.y);
                tmp.x *= (a + tmp2.magnitude);
                tmp.z *= (a + tmp2.magnitude);
                vertices[i] = tmp;
            }
        }

        mesh.vertices = vertices;
        mesh.RecalculateBounds();
        vertices = null;
    }
    
    public void ChangeSphereAngle(float f)
    {
        sphere.transform.localScale = new Vector3(f, sphere.transform.localScale.y, f);
        this.angle = AngleSphereRemap(f);
    }

    public float AngleSphereRemap(float f)
    {
        float OldMax = 10;
        float OldMin = 0;
        float NewMax = 45;
        float NewMin = 0;
        float OldValue = f;

        float OldRange = (OldMax - OldMin);
        float NewRange = (NewMax - NewMin);
        float NewValue = (((OldValue - OldMin) * NewRange) / OldRange) + NewMin;
        return NewValue;
    }

    public void ShapeShift(string s)
    {
        if (s == "single")
        {
            crown.SetActive(true);
            mode = 1;
        }
        else if (s == "double")
        {
            crownTop.SetActive(true);
            crownBot.SetActive(true);
            mode = 2;
        }
        else if (s == "spherical")
        {
            sphere.SetActive(true);
            mode = 3;
        }
    }

    public void ChangeColor(Color c)
    {
        Material editMaterial = new Material(Shader.Find("Unlit/Color"));
        this.crown.GetComponent<Renderer>().material = editMaterial;
        this.crown.GetComponent<Renderer>().material.color = c;
    }

    public void ResetColor()
    {
        this.crown.GetComponent<Renderer>().material = this.originalMaterial;
    }
}