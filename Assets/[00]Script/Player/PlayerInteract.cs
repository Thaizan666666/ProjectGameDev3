using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Yarn.Unity;

public class PlayerInteract : MonoBehaviour
{
    public enum SelectMode { QueueOrder, NearestFirst }
    public SelectMode selectMode = SelectMode.NearestFirst;

    private List<IInteractable> nearbyInteractables = new List<IInteractable>();
    private int currentIndex = 0;
    private IInteractable currentTarget;
    private PlayerInputActions _controls;
    

    private void Awake()
    {
        _controls = new PlayerInputActions();
    }
    void OnEnable()
    {
        _controls.Interacting.Enable();
    }
    void OnDisable()
    {
        _controls.Interacting.Disable();
    }

    void Update()
    {
        HandleScrollInput();
        HandleAutoSelect();
        UpdateHighlight();

        if (currentTarget != null && currentTarget.CanInteract() && /*!dialogueRunner.IsDialogueRunning &&*/ _controls.Interacting.Interact.WasPressedThisFrame())
        {
            currentTarget.Interact();
            _controls.Player.Disable();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // GetComponentInParent (not TryGetComponent) so the trigger collider can live on a
        // child object (e.g. "InteractTrigger") while IInteractable is implemented on the root.
        var interactable = other.GetComponentInParent<IInteractable>();
        if (interactable != null)
        {
            nearbyInteractables.Add(interactable); // ��ͷ��¤�� = ����ҡ�͹����˹���ش
            if (nearbyInteractables.Count == 1)
                currentIndex = 0;
        }
    }

    void OnTriggerExit(Collider other)
    {
        var interactable = other.GetComponentInParent<IInteractable>();
        if (interactable != null)
        {
            int removedIndex = nearbyInteractables.IndexOf(interactable);
            nearbyInteractables.Remove(interactable);

            // ��Ѻ currentIndex ��������ش�ͺ list
            if (nearbyInteractables.Count == 0)
            {
                currentIndex = 0;
            }
            else if (removedIndex <= currentIndex)
            {
                currentIndex = Mathf.Clamp(currentIndex - 1, 0, nearbyInteractables.Count - 1);
            }
        }
    }

    void HandleScrollInput()
    {
        if (nearbyInteractables.Count <= 1) return;

        float scrollY = _controls.Interacting.Select.ReadValue<float>();
        if (scrollY > 0f)
        {
            currentIndex = (currentIndex + 1) % nearbyInteractables.Count;
        }
        else if (scrollY < 0f)
        {
            currentIndex = (currentIndex - 1 + nearbyInteractables.Count) % nearbyInteractables.Count;
        }
    }

    void HandleAutoSelect()
    {
        if (nearbyInteractables.Count == 0)
        {
            currentTarget = null;
            return;
        }

        // ���� NearestFirst �� auto-pick �������ش ��������������� scroll ���͡�ͧ
        if (selectMode == SelectMode.NearestFirst)
        {
            // �� auto-select ੾�е͹�ѧ����� scroll ����¹ manual
            // (������ҧ����Ẻ����: sort �� nearest �ء��������� scroll)
            nearbyInteractables.Sort((a, b) =>
                Vector3.Distance(transform.position, a.GetTransform().position)
                .CompareTo(Vector3.Distance(transform.position, b.GetTransform().position)));
            currentIndex = 0;
        }

        currentTarget = nearbyInteractables[currentIndex];
    }

    void UpdateHighlight()
    {
        for (int i = 0; i < nearbyInteractables.Count; i++)
        {
            nearbyInteractables[i].SetHighlighted(i == currentIndex);
        }
    }
}
