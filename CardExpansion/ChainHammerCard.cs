using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using HeroBang.Game;

namespace HeroBang.CardExpansion
{
  class ChainHammerCard : PlayedCard
  {
    public ChainHammerCard(ICard card)
    {
      this.CardSuit = card.CardSuit;
      this.CardRank = card.CardRank;
      this.CardFunc = Func.ChainHammer;
      this.CardEquippingType = EquippedType.Arm;
    }

    public override bool HasImmediateEffect() { return false; }

    public override void GetDeployed(IPlayer playerReceiving)
    {
      playerReceiving.ReplaceEquippable(this);
    }

    public override void TakeEffect(IPlayer playerPlaying, IPlayer playerReceiving, List<ICard> cardsExtra)
    {
    }

  }
}
