using System.Collections.Generic;
using UnityEngine;

public class FingerGrip : MonoBehaviour
{
    public Transform rightHand;
    public Transform leftHand;

    private Transform[] fingerBones;
    private Quaternion[] savedRotations;

    void Start()
    {
        fingerBones = FindFingerBones();
        SaveFingerRotations();
    }

    Transform[] FindFingerBones()
    {
        var bones = new List<Transform>();
        if (rightHand != null)
            CollectChildren(rightHand, bones);
        if (leftHand != null)
            CollectChildren(leftHand, bones);
        return bones.ToArray();
    }

    void CollectChildren(Transform parent, List<Transform> list)
    {
        foreach (Transform child in parent)
        {
            list.Add(child);
            CollectChildren(child, list);
        }
    }

    [ContextMenu("Save Current Finger Rotations")]
    public void SaveFingerRotations()
    {
        if (fingerBones == null) fingerBones = FindFingerBones();
        savedRotations = new Quaternion[fingerBones.Length];
        for (int i = 0; i < fingerBones.Length; i++)
        {
            if (fingerBones[i] != null)
                savedRotations[i] = fingerBones[i].localRotation;
        }
    }

    void LateUpdate()
    {
        if (savedRotations == null || fingerBones == null) return;
        for (int i = 0; i < fingerBones.Length; i++)
        {
            if (fingerBones[i] != null && i < savedRotations.Length)
                fingerBones[i].localRotation = savedRotations[i];
        }
    }
}
