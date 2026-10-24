using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The log of the trades that went through: game day, town, good, quantity and amount, the most recent ones kept
/// (the oldest are dropped past the limit). It listens to the trade service and changes nothing in the game.
/// Each entry can also be written in the console.
/// </summary>
public class TradeJournal : MonoBehaviour
{
    [SerializeField] private TradeService tradeService;
    [Tooltip("How many trades are kept; past that the oldest are dropped.")]
    [Min(1)] [SerializeField] private int maxEntries = 500;
    [SerializeField] private bool logToConsole = true;

    private readonly List<TradeRecord> records = new List<TradeRecord>();

    /// <summary>Raised after a trade was added to the journal.</summary>
    public event Action<TradeRecord> Recorded = delegate { };

    public IReadOnlyList<TradeRecord> Records => records;

    private void Start()
    {
        if (tradeService == null)
        {
            Debug.LogError("TradeJournal: the trade service is not assigned.", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        if (tradeService != null)
        {
            tradeService.Traded += OnTraded;
        }
    }

    private void OnDisable()
    {
        if (tradeService != null)
        {
            tradeService.Traded -= OnTraded;
        }
    }

    private void OnTraded(TradeRecord record)
    {
        if (records.Count >= maxEntries)
        {
            records.RemoveAt(0);
        }

        records.Add(record);
        if (logToConsole)
        {
            Debug.Log("[Commerce] Jour " + record.Day + " | " + record.TownName + " | "
                + (record.Kind == TradeKind.Buy ? "achat " : "vente ") + record.Quantity + " " + record.Good.DisplayName
                + " : " + MoneyFormat.WithUnit(record.Amount), this);
        }

        Recorded(record);
    }
}
