using HarmonyLib;
using Kitchen.ONe.Tweak.Config.Sections;
using KitchenData;
using Unity.Entities;

namespace Kitchen.ONe.Tweak.Tweaks;

/// <summary>
/// This is an experimental tweak that makes it possible to choose from additional cards.
/// </summary>
public static class SelectFromMoreCardsTweak
{
    private static bool _showNextCard;
    private static bool _refreshPopup;
    
    public static void ShowNextCard()
    {
        _showNextCard = true;
    }
    
    /// <summary>
    /// This system creates additional CProgressionRequest requests.
    /// </summary>
    [UpdateInGroup(typeof (EndOfDayProgressionGroup), OrderFirst = true)]
    [UpdateAfter(typeof(CreateProgressionRequests))]
    private class SelectMoreCards : StartOfNightSystem
    {
        private EntityQuery _singletonEntityQuerySDay54;
        
        protected override void Initialise()
        {
            base.Initialise();
            this._singletonEntityQuerySDay54 = this.GetEntityQuery(ComponentType.ReadOnly<SDay>());
        }

        protected override void OnUpdate()
        {
            if (!CardsConfig.Instance.EnableAdditionalCards.Value)
            {
                return;
            }
            
            if (this.HasSingleton<SIsRestartedDay>())
                return;
            
            var day = this._singletonEntityQuerySDay54.GetSingleton<SDay>().Day;
            
            if (!this.HasSingleton<CreateNewKitchen.SKitchenFirstFrame>() && day % 3 == 0 && day != 15)
            {
                for (int i = 0; i < CardsConfig.Instance.AdditionalCustomerDishCards.Value; i++)
                {
                    if (i % 2 == 0)
                    {
                        this.AddRequest(UnlockGroup.Generic);
                    }
                    else
                    {
                        this.AddRequest(UnlockGroup.Dish);
                    }
                }
            }
            if (day == 15)
            {
                for (int i = 0; i < CardsConfig.Instance.AdditionalFranchiseCards.Value; i++)
                {
                    this.AddRequest(UnlockGroup.FranchiseCard);
                }
            }
        }
    
        private void AddRequest(UnlockGroup group) => this.EntityManager.SetComponentData<CProgressionRequest>(this.EntityManager.CreateEntity((ComponentType) typeof (CProgressionRequest)), new CProgressionRequest()
        {
            Group = group
        });
    }
    
    /// <summary>
    /// This system makes it possible to cycle through all the available cards.
    /// </summary>
    private class CycleCards : NightSystem
    {
        protected override void OnUpdate()
        {
            if (!_showNextCard)
            {
                return;
            }

            _showNextCard = false;
            
            var entities = GetEntityQuery(typeof(CUnlockSelectPopup));
    
            if (entities.IsEmpty)
            {
                return;
            }
    
            var popupEntity = entities.First();
            var buffer = EntityManager.GetBuffer<CUnlockSelectPopupOption>(popupEntity);

            var tmp = buffer[0];
            for (var index = 0; index < buffer.Length; index++)
            {
                if (index != buffer.Length - 1)
                {
                    buffer[index] = buffer[index + 1];
                }
                else
                {
                    buffer[index] = tmp;
                }
            }

            _refreshPopup = true;
        }
    }
    
    /// <summary>
    /// This system refreshes the popup when a different pair of cards should be shown.
    /// </summary>
    [HarmonyPatch(typeof(UnlockSelectPopupView), "UpdateData")] 
    private static class UnlockSelectPopupViewPatch
    {
        public static void Prefix(ref bool ___IsUpdated)
        {
            if (!_refreshPopup)
            {
                return;
            }
            
            _refreshPopup = false;
            ___IsUpdated = false;
        }
    }
}