using System.Collections;
using System.Collections.Generic;
using ScriptableObjects;
using UnityEngine;
using ZetanStudio.DialogueSystem;

[CreateAssetMenu(fileName = "Interactable", menuName = "Interactable/Interactable", order = 0)]
public class InteractableScriptableObject : ScriptableObject
{
    public Dialogue heldDialogue;
    public string interactableName;
    public bool canPickUp;
    public Sprite hoveredSprite;
    public Sprite normalSprite;
    public float horizontalOffset;
    public float verticalOffset;
}
