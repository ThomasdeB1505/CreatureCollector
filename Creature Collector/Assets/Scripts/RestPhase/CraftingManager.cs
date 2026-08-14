using System;
using System.Collections.Generic;
using UnityEngine;

public class CraftingManager : MonoBehaviour
{
    public static CraftingManager Instance;

    [Header("Recipes")]
    public CreatureRecipe[] recipes; // 9 entries, one per Form x Essence combo

    [Header("UI")]
    public CraftingHubUI hubUI;

    private Dictionary<FormType, int> formInventory = new();
    private Dictionary<EssenceType, int> essenceInventory = new();

    private Action onHubDone;

    void Awake()
    {
        Instance = this;
        foreach (FormType f in Enum.GetValues(typeof(FormType))) formInventory[f] = 0;
        foreach (EssenceType e in Enum.GetValues(typeof(EssenceType))) essenceInventory[e] = 0;
    }

    public void AddForm(FormType form) => formInventory[form]++;
    public void AddEssence(EssenceType essence) => essenceInventory[essence]++;

    public int GetFormCount(FormType f) => formInventory[f];
    public int GetEssenceCount(EssenceType e) => essenceInventory[e];

    // ── Forced initial craft (game start) ──────────────────────────────────
    public void ForceInitialCraft(FormType startingForm, Action onComplete)
    {
        onHubDone = onComplete;
        AddForm(startingForm);
        hubUI.ShowForcedInitialCraft(this, startingForm, chosenEssence =>
        {
            AddEssence(chosenEssence);
            Craft(startingForm, chosenEssence);
            onHubDone?.Invoke();
        });
    }

    // ── Normal between-encounter hub ────────────────────────────────────────
    public void ShowHub(Action onDone)
    {
        onHubDone = onDone;
        hubUI.Show(this, OnHubClosed);
    }

    void OnHubClosed()
    {
        onHubDone?.Invoke();
    }

    // ── Crafting ─────────────────────────────────────────────────────────────
    public bool CanCraft(FormType form, EssenceType essence)
    {
        return formInventory[form] > 0 && essenceInventory[essence] > 0;
    }
    public bool CanCraftAnything(FormType form, EssenceType essence)
    {
        return !PlayerRoster.Instance.IsFull && CanCraft(form, essence);
    }


    public GameObject Craft(FormType form, EssenceType essence, bool consumeResources = true)
    {
        if (PlayerRoster.Instance.IsFull) return null; // can't craft with a full roster

        if (consumeResources)
        {
            if (!CanCraft(form, essence)) return null;
            formInventory[form]--;
            essenceInventory[essence]--;
        }

        var recipe = Array.Find(recipes, r => r.form == form && r.essence == essence);
        if (recipe == null)
        {
            Debug.LogWarning($"No recipe found for {form} + {essence}");
            return null;
        }

        PlayerRoster.Instance.AddCreature(recipe.resultPrefab);
        return recipe.resultPrefab;
    }
}