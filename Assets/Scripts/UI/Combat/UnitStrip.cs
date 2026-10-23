using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A row of small cards, one per living unit of the player's side: name, hit points, what it is doing. The selected
/// ones are highlighted and a click on a card selects that unit. The cards are copies of a template object; the
/// row is rebuilt only when the set of units changes (a unit died).
/// </summary>
public class UnitStrip : MonoBehaviour
{
    private class Card
    {
        public SelectableUnit Unit;
        public Health Health;
        public Image Background;
        public Text Name;
        public Text Info;
        public RectTransform Fill;
    }

    [SerializeField] private SelectionManager selection;
    [Tooltip("An inactive card used as a model: a Button with an Image and, among its children, Name, Info and Fill.")]
    [SerializeField] private Button cardTemplate;
    [Tooltip("Where the cards are put (it should have a horizontal layout group).")]
    [SerializeField] private RectTransform container;
    [SerializeField] private Color normalColor = new Color(0.18f, 0.2f, 0.25f, 0.9f);
    [SerializeField] private Color selectedColor = new Color(0.2f, 0.45f, 0.75f, 0.95f);
    [Min(0.02f)] [SerializeField] private float refreshInterval = 0.15f;

    private readonly List<Card> cards = new List<Card>();
    private readonly List<SelectableUnit> found = new List<SelectableUnit>();
    private float nextRefresh;

    private void Start()
    {
        if (selection == null || cardTemplate == null || container == null)
        {
            Debug.LogError("UnitStrip: a serialized reference is not assigned.", this);
            enabled = false;
            return;
        }

        cardTemplate.gameObject.SetActive(false);
        Refresh();
    }

    private void OnEnable()
    {
        if (selection != null)
        {
            selection.SelectionChanged += OnSelectionChanged;
        }
    }

    private void OnDisable()
    {
        if (selection != null)
        {
            selection.SelectionChanged -= OnSelectionChanged;
        }
    }

    private void Update()
    {
        if (Time.unscaledTime < nextRefresh)
        {
            return;
        }

        nextRefresh = Time.unscaledTime + refreshInterval;
        Refresh();
    }

    private void OnSelectionChanged()
    {
        Refresh();
    }

    private void Refresh()
    {
        FindUnits();
        if (!SameUnits())
        {
            Rebuild();
        }

        for (int i = 0; i < cards.Count; i++)
        {
            Card card = cards[i];
            card.Background.color = card.Unit.IsSelected ? selectedColor : normalColor;
            card.Name.text = card.Unit.name;
            card.Info.text = "PV " + card.Health.Current + "/" + card.Health.Max;
            card.Fill.anchorMax = new Vector2(card.Health.Max > 0 ? Mathf.Clamp01((float)card.Health.Current / card.Health.Max) : 0f, 1f);
        }
    }

    // The living units of the player's side, in the scene's order.
    private void FindUnits()
    {
        found.Clear();
        IReadOnlyList<SelectableUnit> all = SelectableUnit.All;
        for (int i = 0; i < all.Count; i++)
        {
            UnitTeam unitTeam = all[i].GetComponent<UnitTeam>();
            Health health = all[i].GetComponent<Health>();
            if (unitTeam != null && unitTeam.Team == Team.Player && health != null && !health.IsDead)
            {
                found.Add(all[i]);
            }
        }
    }

    private bool SameUnits()
    {
        if (found.Count != cards.Count)
        {
            return false;
        }

        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i].Unit != found[i])
            {
                return false;
            }
        }

        return true;
    }

    private void Rebuild()
    {
        for (int i = 0; i < cards.Count; i++)
        {
            Destroy(cards[i].Background.gameObject);
        }

        cards.Clear();
        for (int i = 0; i < found.Count; i++)
        {
            Button button = Instantiate(cardTemplate, container);
            button.gameObject.SetActive(true);
            var card = new Card
            {
                Unit = found[i],
                Health = found[i].GetComponent<Health>(),
                Background = button.GetComponent<Image>(),
                Name = button.transform.Find("Name").GetComponent<Text>(),
                Info = button.transform.Find("Info").GetComponent<Text>(),
                Fill = (RectTransform)button.transform.Find("HealthBack/Fill")
            };
            SelectableUnit unit = found[i];
            button.onClick.AddListener(() => selection.SelectOnly(unit));
            cards.Add(card);
        }
    }
}
