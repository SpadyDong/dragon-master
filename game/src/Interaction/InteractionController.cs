using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 交互控制器 — 挂在 Player 上，检测范围内的 Interactable 并按 F 触发
/// </summary>
public class InteractionController : MonoBehaviour
{
    [Header("检测")]
    [SerializeField] private float detectRadius = 3f;
    [SerializeField] private LayerMask interactableLayer = ~0;

    [Header("UI")]
    [SerializeField] private InteractionPrompt promptUI;

    private Interactable _currentTarget;
    private readonly List<Interactable> _nearby = new();
    private PlayerController _player;

    void Awake()
    {
        _player = GetComponent<PlayerController>();
    }

    void Update()
    {
        if (GameManager.Instance == null) return;

        // 仅在 Playing 状态检测交互
        if (GameManager.Instance.CurrentState != GameState.Playing)
        {
            HidePrompt();
            return;
        }

        DetectInteractables();

        // 显示/隐藏提示
        if (_currentTarget != null)
        {
            ShowPrompt(_currentTarget.promptText);
        }
        else
        {
            HidePrompt();
        }

        // F 键触发交互
        if (InputManager.Instance != null && InputManager.Instance.ConsumeInteract())
        {
            if (_currentTarget != null && _currentTarget.CanInteract(_player))
            {
                _currentTarget.OnInteract(_player);
            }
        }
    }

    private void DetectInteractables()
    {
        _nearby.Clear();
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectRadius, interactableLayer);

        Interactable best = null;
        float bestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent(out Interactable inter))
            {
                float dist = Vector2.Distance(transform.position, inter.transform.position);
                if (dist <= inter.interactRange && inter.CanInteract(_player))
                {
                    _nearby.Add(inter);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        best = inter;
                    }
                }
            }
        }

        _currentTarget = best;
    }

    private void ShowPrompt(string text)
    {
        if (promptUI != null)
        {
            promptUI.Show(text, _currentTarget != null ? _currentTarget.transform.position + Vector3.up * 1.5f : transform.position);
        }
    }

    private void HidePrompt()
    {
        if (promptUI != null)
            promptUI.Hide();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRadius);
    }
}
