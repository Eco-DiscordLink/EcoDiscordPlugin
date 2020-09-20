using DSharpPlus.Entities;
using Eco.Core.Systems;
using Eco.Gameplay.Economy.WorkParties;
using Eco.Gameplay.Items;
using Eco.Plugins.DiscordLink.Utilities;
using Eco.Shared.Items;
using Eco.Shared.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Eco.Plugins.DiscordLink.IntegrationTypes
{
    public class WorkPartyDisplay : Display
    {
        protected override string BaseTag { get { return "[Work Party]"; } }
        protected override int TimerUpdateIntervalMS { get { return 60000; } }
        protected override int TimerStartDelayMS { get { return 10000; } }

        protected override TriggerType GetTriggers()
        {
            return TriggerType.Startup | TriggerType.Timer | TriggerType.PostedWorkParty | TriggerType.CompletedWorkParty
                | TriggerType.JoinedWorkParty | TriggerType.LeftWorkParty | TriggerType.WorkedWorkParty;
        }

        protected override List<ChannelLink> GetChannelLinks()
        {
            return DLConfig.Data.WorkPartyChannels.ToList();
        }

        protected override void GetDisplayContent(ChannelLink link, out List<Tuple<string, DiscordEmbed>> tagAndContent)
        {
            tagAndContent = new List<Tuple<string, DiscordEmbed>>();
            DiscordEmbedBuilder builder = new DiscordEmbedBuilder();
            List<WorkParty> workParties = Registrars.Get<WorkParty>().All<WorkParty>().NonNull().Where(x => x.State == ProposableState.Active).ToList();
            foreach (WorkParty workParty in workParties)
            {
                string tag = BaseTag + " [" + workParty.Id + "]";
                builder.WithColor(MessageBuilder.EmbedColor);
                builder.WithTitle(workParty.Name);

                // Workers
                string workersDesc = string.Empty;
                foreach(Laborer laborer in workParty.Laborers)
                {
                    if (laborer.Citizen == null) continue;
                    bool isCreator = laborer.Citizen == workParty.Creator;
                    workersDesc += laborer.Citizen.Name + (isCreator ? " (Creator)" : string.Empty) + "\n";
                }

                if (string.IsNullOrWhiteSpace(workersDesc))
                {
                    workersDesc += "--- No Workers Registered ---";
                }
                builder.AddField("Workers", workersDesc);

                // Work
                foreach (Work work in workParty.Work)
                {
                    string workDesc = string.Empty;
                    string workType = string.Empty;
                    List<string> workEntries = new List<string>();
                    switch(work)
                    {
                        case LaborWork laborWork:
                            {
                                if (!string.IsNullOrEmpty(laborWork.ShortDescriptionRemainingWork))
                                {
                                    workType = "Labor for " + laborWork.Order.Recipe.RecipeName;
                                    workEntries.Add(MessageUtil.StripTags(laborWork.ShortDescriptionRemainingWork));
                                }
                                break;
                            }

                        case WorkOrderWork orderWork:
                            {
                                workType = "Materials for " + orderWork.Order.Recipe.RecipeName;
                                foreach (TagStack stack in orderWork.Order.MissingIngredients)
                                {
                                    string itemName = string.Empty;
                                    if (stack.Item != null)
                                    {
                                        itemName = stack.Item.DisplayName;
                                    }
                                    else if (stack.StackObject != null)
                                    {
                                        itemName = stack.StackObject.DisplayName;
                                    }
                                    workEntries.Add(itemName + " (" + stack.Quantity + ")");
                                }
                                break;
                            }

                        default:
                            break;
                    }

                    if (workEntries.Count > 0)
                    {
                        foreach (string material in workEntries)
                        {
                            workDesc += "- " + material + "\n";
                        }

                        if (!string.IsNullOrWhiteSpace(workDesc))
                        {
                            string percentDone = (work.PercentDone * 100.0f).ToString("N1", CultureInfo.InvariantCulture).Replace(".0", "");
                            builder.AddField("\n" + workType + " (Weight " + work.Weight + ") (" + percentDone + "% completed) \n", workDesc);
                        }
                    }
                }

                // Payment
                string paymentDesc = string.Empty;
                foreach(Payment payment in workParty.Payment)
                {
                    string desc = string.Empty;
                    switch(payment)
                    {
                        case CurrencyPayment currencyPayment:
                            {
                                float currencyAmountLeft = currencyPayment.Amount - currencyPayment.AmountPaid;
                                if (currencyAmountLeft > 0.0f)
                                {
                                    desc = "Receive " + currencyAmountLeft + " " + currencyPayment.Currency.Name
                                        + (currencyPayment.PayType == PayType.SplitByWorkPercent ? ", split based on work performed" : ", split evenly")
                                        + (currencyPayment.PayAsYouGo ? ", paid as work is performed" : ", paid when the project finishes");
                                }
                                break;
                            }

                        case GrantTitlePayment titlePayment:
                            {
                                desc = "Receive title `" + titlePayment.Title.Name + "` if work contributed is at least " + titlePayment.MinContributedPercent + "%";
                                break;
                            }

                        case KnowledgeSharePayment knowledgePayment:
                            {
                                if (knowledgePayment.Skills.Entries.Count > 0)
                                {
                                    desc = "Receive knowledge of `" + MessageUtil.StripTags(knowledgePayment.ShortDescription()) + "` if work contributed is at least " + knowledgePayment.MinContributedPercent + "%";
                                }
                                break;
                            }

                        case ReputationPayment reputationPayment:
                            {
                                float reputationAmountLeft = reputationPayment.Amount - reputationPayment.AmountPaid;
                                desc = "Receive " + reputationAmountLeft + " reputation from " + reputationPayment.WorkParty.Creator.Name
                                    + (reputationPayment.PayType == PayType.SplitByWorkPercent ? ", split based on work performed" : ", split evenly")
                                        + (reputationPayment.PayAsYouGo ? ", paid as work is performed" : ", paid when the project finishes");
                                break;
                            }

                        default:
                            break;
                    }

                    if (!string.IsNullOrEmpty(desc))
                    {
                        paymentDesc += "- " + desc + "\n";
                    }
                }

                if (!string.IsNullOrWhiteSpace(paymentDesc))
                {
                    builder.AddField("Payment", paymentDesc);
                }

                if (builder.Fields.Count > 0)
                {
                    tagAndContent.Add(new Tuple<string, DiscordEmbed>(tag, builder.Build()));
                }
                builder.ClearFields();
            }
        }
    }
}
