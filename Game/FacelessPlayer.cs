using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using HeroBang.CardExpansion;

namespace HeroBang.Game
{
  public class FacelessPlayer : IPlayer
  {
    public IFigure PlayerFigure { get; set; }

    public List<ICard> CardCarrying { get; set; }
    public bool InPrison { get; set; }
    public List<ICard> CardEquipped { get; set; }

    public ICard CardPlayed { get; set; }
    public ICard CardResponded { get; set; }
    public ICard CardNeedResponse { get; set; }

    public List<ICard> CardsHeld { get; set; }
    public List<ICard> CardsCanBePlayedInThisRound { get; set; }
    public List<ICard> CardsPlayedInThisRound { get; set; }

    // TODO Both these variables need to become lists
    public IPlayer PlayerToKill { get; set; }
    public IPlayer PlayerToProtect { get; set; }

    PlayerMessenger messenger;
    public PlayerMessenger Messenger { get { return messenger; } }

    public FacelessPlayer(FacelessFigure initialFigure, CardTable initialTable)
    {
      PlayerFigure = initialFigure;

      messenger = new PlayerMessenger();
      messenger.PlayerTurnStarts += new PlayerMessenger.PlayerTurnStartsEventHandler(Messenger_PlayerTurnStarts);
      messenger.PlayAnotheCard += new PlayerMessenger.PlayAnotheCardEventHandler(Messenger_PlayAnotheCard);
      messenger.ResponseNeeded += new PlayerMessenger.ResponseNeededEventHandler(Messenger_ResponseNeeded);
      messenger.PlayerTurnEnds += new PlayerMessenger.PlayerTurnEndsEventHandler(Messenger_PlayerTurnEnds);

      CardCarrying = new List<ICard>();
      CardEquipped = new List<ICard>();

      CardsPlayedInThisRound = new List<ICard>();
      CardsCanBePlayedInThisRound = new List<ICard>();
    }

    ICard PlayACard(ICard card)
    {
      return PlayACard(card, null, false, false);
    }

    ICard PlayACard(ICard card, IPlayer playerReceiving, bool isAnnulling, bool fromEquipped)
    {
      if (card == null)
        return null;

      if (fromEquipped)
        CardEquipped.Remove(card);
      else
        CardsHeld.Remove(card);
      CardsPlayedInThisRound.Add(card);
      card.PlayerPlaying = this;
      card.PlayerReceiving = playerReceiving ?? this;
      card.IsAnnulling = isAnnulling;

      return card;
    }

    //public ICard PlayACardFromEquipped(ICard cardFirst, IPlayer playerReceiving, bool isAnnulling)
    //{
    //  ICard card = null; 
    //  if (cardFirst.CardFunc == Func.Kill)
    //    card = CardEquipped.Find(c => c.CardFunc == Func.Jade);

    //  if (card == null)
    //    return null;

    //  CardsPlayedInThisRound.Add(card);
    //  card.PlayerPlaying = this;
    //  card.PlayerReceiving = playerReceiving ?? this;
    //  card.IsAnnulling = isAnnulling;

    //  return card;
    //}

    public void PrintPlayedCard(ICard card)
    {
      Util.PrintPlayerAction(this, card, PlayerAction.Play);
    }

    void DecideCardsCanBePlayedInYourTurn()
    {
      CardsCanBePlayedInThisRound.Clear();

      // Cannot play any card if the player is in prison
      if (InPrison)
        return;

      foreach (ICard card in CardsHeld)
      {
        if (CardJudge.CanPlayerPlayCard(this, this.PlayerToKill, card))
          CardsCanBePlayedInThisRound.Add(card);
      }
    }

    ICard DecideWhatCardToPlayInYourTurn()
    {
      ICard card;

      // TODO Think about the big picture - Advanced 
      card = PlayCardBasedOnBigPicture();
      if (card != null)
      {
        return PlayACard(card);
      }

      // Heal yourself and your alliance
      card = PlayCardToHeal();
      if (card != null)
      {
        // TODO The player does not always heal himself
        return PlayACard(card);
      }

      // Play beneficial cards for yourself and your alliance
      card = PlayBeneficialCard();
      if (card != null)
      {
        return PlayACard(card);
      }

      // Equip yourself and your alliance
      card = PlayEquippableCard();
      if (card != null)
      {
        return PlayACard(card);
      }

      // No player to kill
      if (PlayerToKill == null)
        return null;

      card = PlayStealingCard();
      if (card != null)
      {
        return PlayACard(card);
      }

      card = PlayOffensiveCard();
      if (card != null)
      {
        return PlayACard(card);
      }

      // Use the bomb
      card = PlayBombCard();
      if (card != null)
      {
        return PlayACard(card);
      }

      // Use the prison
      card = PlayPrisonCard();
      if (card != null)
      {
        return PlayACard(card, PlayerToKill, false, false); // TODO The player to kill is not always the same as the player to prison
      }

      // TODO No player to protect

      return null;
    }

    void Messenger_PlayerTurnStarts(PlayerMessenger messenger)
    {
      Messenger_PlayAnotheCard(messenger);
    }

    void Messenger_PlayAnotheCard(PlayerMessenger messenger)
    {
      DecideCardsCanBePlayedInYourTurn();
      this.CardPlayed = this.CardNeedResponse = DecideWhatCardToPlayInYourTurn();
    }

    void Messenger_ResponseNeeded(PlayerMessenger messenger, SinglePlayerResponseArgs e)
    {
      this.CardResponded = DecideWhatCardToPlayOutOfYourTurn(e);
    }

    void Messenger_PlayerTurnEnds(PlayerMessenger messenger)
    {
      FinishYourRound();
    }
  
    ICard DecideWhatCardToPlayOutOfYourTurn(SinglePlayerResponseArgs e)
    {
      ICard cardTriggering = e.CardTriggering;
      List<ICard> miniCardList = e.MiniCardList;
      ICard cardFirst = e.CardFirst;

      List<Func> funcsCanCounter;
      List<Func> funcsCanAnnul;
      ICard cardResponded = null;

      //if (CardJudge.CardCanBeAnnulledByEquippable(null, cardFirst, cardTriggering, out funcsCanAnnul) != MiniRoundTimingOption.None)
      //  cardResponded = PlayACard(this.FindASpecificCard(funcsCanAnnul), PlayerToKill, true);

      // TODO Make the function parameters correct
      if (CardJudge.CanBePossiblyAnnulledByEquippable(cardFirst.PlayerPlaying, cardFirst, out funcsCanAnnul) != MiniRoundTimingOption.None)
        cardResponded = PlayACard(this.FindASpecificCardInEquippable(funcsCanAnnul), PlayerToKill, true, true);

      if (cardResponded != null)
        return cardResponded;

      // TODO Add the logic about whether I want to intervene
      if (CardJudge.CardCanBeCounteredByAnyPlayer(null, cardFirst, cardTriggering, out funcsCanCounter) != MiniRoundTimingOption.None)
        cardResponded = PlayACard(this.FindASpecificCard(funcsCanCounter), PlayerToKill, false, false);

      if (cardResponded != null)
        return cardResponded;

      if (CardJudge.CardCanBeAnnulled(null, cardFirst, cardTriggering, out funcsCanAnnul) != MiniRoundTimingOption.None)
        cardResponded = PlayACard(this.FindASpecificCard(funcsCanAnnul), PlayerToKill, true, false);

      if (cardResponded != null)
        return cardResponded;

      return null; // Make no response
    }

    ICard PlayCardBasedOnBigPicture()
    {
      return null;
    }

    ICard PlayCardToHeal()
    {
      //if (!PlayerFigure.NeedHealing())
      //  return null;

      // TODO Need to consider if your opponent/alliance needs to be healed 
      // and if you have the card ParryAll
      ICard cardHealingAll = CardsCanBePlayedInThisRound.Find(c => c.CardFunc == Func.HealingAll);
      if (cardHealingAll != null)
        return cardHealingAll;

      ICard cardPortion = CardsCanBePlayedInThisRound.Find(c => c.CardFunc == Func.Portion);
      if (cardPortion != null)
        return cardPortion;

      return null;
    }

    ICard PlayBeneficialCard()
    {
      // TODO Make all Play***Card functions like this
      return CardsCanBePlayedInThisRound.Find(c => c.IsBeneficial());
    }

    ICard PlayEquippableCard()
    {
      ICard cardEquippable = CardsCanBePlayedInThisRound.Find(c => c.IsEquippable());
      
      if (cardEquippable != null)
      {
        cardEquippable.CardAction = CAction.GetEquipped;
        return cardEquippable;
      }
        
      return null;
    }

    ICard PlayStealingCard()
    {
      List<ICard > cardsStealing = CardsCanBePlayedInThisRound.FindAll(c => c.IsStealing());
      if (cardsStealing == null)
        return null;
      
      ICard cardPocketPicking = cardsStealing.Find(c => c.CardFunc == Func.PocketPicking);
      if (cardPocketPicking != null)
        return cardPocketPicking;

      ICard cardPickAndDrop = cardsStealing.Find(c => c.CardFunc == Func.PickAndDrop);
      if (cardPickAndDrop != null)
        return cardPickAndDrop;

      return null;
    }

    ICard PlayOffensiveCard()
    {
      List<ICard> cardsOffensive = CardsCanBePlayedInThisRound.FindAll(c => c.IsOffensive());
      if (cardsOffensive == null)
        return null;

      ICard cardBarbariansAtGate = cardsOffensive.Find(c => c.CardFunc == Func.BarbariansAtGate);
      if (cardBarbariansAtGate != null)
        return cardBarbariansAtGate;

      ICard cardTenKArrows = cardsOffensive.Find(c => c.CardFunc == Func.TenKArrows);
      if (cardTenKArrows != null)
        return cardTenKArrows;

      ICard cardDuel = cardsOffensive.Find(c => c.CardFunc == Func.Duel);
      if (cardDuel != null)
        return cardDuel;

      ICard cardKill = cardsOffensive.Find(c => c.CardFunc == Func.Kill);
      if (cardKill != null)
        return cardKill;

      return null;
    }

    ICard PlayBombCard()
    {
      ICard cardBomb = CardsCanBePlayedInThisRound.Find(c => c.IsBomb());
      if (cardBomb != null)
        return cardBomb;

      return null;
    }

    ICard PlayPrisonCard()
    {
      ICard cardPrison = CardsCanBePlayedInThisRound.Find(c => c.IsPrison());
      if (cardPrison != null)
        return cardPrison;

      return null;
    }

    void FinishYourRound()
    {
      CardsPlayedInThisRound.Clear();
      CardsCanBePlayedInThisRound.Clear();
      CardPlayed = null;
      CardResponded = null;
      CardNeedResponse = null;
      InPrison = false;
    }

  }
}
