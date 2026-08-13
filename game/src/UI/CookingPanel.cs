using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 烹饪面板 — 已解锁食谱列表 + 烹饪
/// </summary>
public class CookingPanel : MonoBehaviour
{
    [Header("食谱列表")]
    [SerializeField] private Transform recipeListContainer;
    [SerializeField] private GameObject recipeEntryPrefab;
    [SerializeField] private GameObject emptyHint;

    [Header("关闭")]
    [SerializeField] private Button closeButton;

    private List<GameObject> _entryObjects = new();

    void Start()
    {
        if (closeButton != null) closeButton.onClick.AddListener(OnCloseClicked);
    }

    void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (CookingManager.Instance == null) return;
        if (recipeListContainer == null || recipeEntryPrefab == null) return;

        foreach (var obj in _entryObjects) Destroy(obj);
        _entryObjects.Clear();

        var recipeIds = CookingManager.Instance.GetAllKnownRecipes();
        if (emptyHint != null) emptyHint.SetActive(recipeIds.Count == 0);

        foreach (var recipeId in recipeIds)
        {
            var recipe = Resources.Load<RecipeData>($"Recipes/{recipeId}");
            if (recipe == null) continue;

            var entry = Instantiate(recipeEntryPrefab, recipeListContainer);
            _entryObjects.Add(entry);

            var nameText = entry.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            var descText = entry.transform.Find("DescText")?.GetComponent<TextMeshProUGUI>();
            var cookButton = entry.transform.Find("CookButton")?.GetComponent<Button>();

            if (nameText != null) nameText.text = recipe.recipeName;
            if (descText != null)
            {
                string ingredients = "";
                foreach (var ing in recipe.ingredients)
                    ingredients += $"{ing.itemId}×{ing.count} ";
                descText.text = $"材料: {ingredients}";
            }

            if (cookButton != null)
            {
                string capturedId = recipeId;
                bool canCook = CookingManager.Instance.CanCook(recipeId);
                cookButton.interactable = canCook;
                cookButton.onClick.RemoveAllListeners();
                cookButton.onClick.AddListener(() =>
                {
                    CookingManager.Instance.Cook(capturedId);
                    Refresh();
                });
            }
        }
    }

    private void OnCloseClicked()
    {
        UIManager.Instance?.CloseStandalonePanels();
    }
}
