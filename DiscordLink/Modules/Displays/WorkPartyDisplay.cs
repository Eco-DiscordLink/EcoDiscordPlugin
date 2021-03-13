using DiscordLink.Extensions;
using DSharpPlus.Entities;
using Eco.Core.Systems;
using Eco.Gameplay.Economy.WorkParties;
using Eco.Gameplay.Items;
using Eco.Plugins.DiscordLink.Events;
using Eco.Plugins.DiscordLink.Utilities;
using Eco.Shared.Items;
using Eco.Shared.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Eco.Plugins.DiscordLink.Modules
{
    public class WorkPartyDisplay : Display
    {
        protected override string BaseTag { get { return "[Work Party]"; } }
        protected override int TimerUpdateIntervalMS { get { return 60000; } }
        protected override int TimerStartDelayMS { get { return 10000; } }

        public override string ToString()
        {
            return "Work Party Display";
        }

        protected override DLEventType GetTriggers()
        {
            return DLEventType.DiscordClientStarted | DLEventType.Timer | DLEventType.PostedWorkParty | DLEventType.CompletedWorkParty
                | DLEventType.JoinedWorkParty | DLEventType.LeftWorkParty | DLEventType.WorkedWorkParty;
        }

        protected override List<DiscordTarget> GetDiscordTargets()
        {
            return DLConfig.Data.WorkPartyDisplayChannels.Cast<DiscordTarget>().ToList();
        }

        protected override void GetDisplayContent(DiscordTarget target, out List<Tuple<string, DiscordLinkEmbed>> tagAndContent)
        {
            tagAndContent = new List<Tuple<string, DiscordLinkEmbed>>();
            DiscordLinkEmbed embed = new DiscordLinkEmbed();
            List<WorkParty> workParties = Registrars.Get<WorkParty>().All<WorkParty>().NonNull().Where(x => x.State == ProposableState.Active).ToList();
            foreach (WorkParty workParty in workParties)
            {
                string tag = $"{BaseTag} [{workParty.Id}]";
                embed.WithTitle(MessageUtil.StripTags(workParty.Name));
                embed.WithFooter(MessageBuilder.Discord.GetStandardEmbedFooter());

                // Workers
                string workersDesc = string.Empty;
                foreach(Laborer laborer in workParty.Laborers)
                {
                    if (laborer.Citizen == null) continue;
                    string creator = (laborer.Citizen == workParty.Creator) ? "Creator" : string.Empty;
                    workersDesc += $"{laborer.Citizen.Name} ({creator})\n";
                }

                if (string.IsNullOrWhiteSpace(workersDesc))
                {
                    workersDesc += "--- No Workers Registered ---";
                }
                embed.AddField("Workers", workersDesc);

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
                                    workType = $"Labor for {laborWork.Order.Recipe.RecipeName}";
                                    workEntries.Add(MessageUtil.StripTags(laborWork.ShortDescriptionRemainingWork));
                                }
                                break;
                            }

                        case WorkOrderWork orderWork:
                            {
                                workType = $"Materials for {orderWork.Order.Recipe.RecipeName}";
                                foreach (TagStack stack in orderWork.Order.MissingIngredients)
                                {
                                    string itemName = string.Empty;
                                    if (stack.Item != null)
                                        itemName = stack.Item.DisplayName;
                                    else if (stack.StackObject != null)
                                        itemName = stack.StackObject.DisplayName;
                                    workEntries.Add($"{itemName} ({stack.Quantity})");
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
                            workDesc += $"- {material}\n";
                        }

                        if (!string.IsNullOrWhiteSpace(workDesc))
                        {
                            string percentDone = (work.PercentDone * 100.0f).ToString("N1", CultureInfo.InvariantCulture).Replace(".0", "");
                            embed.AddField($"\n {workType} (Weight: {work.Weight.ToString("F1")}) ({percentDone}% completed) \n", workDesc);
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
                                    desc = $"Receive **{currencyAmountLeft.ToString("F1")} {currencyPayment.Currency.Name}**"
                                        + (currencyPayment.PayType == PayType.SplitByWorkPercent ? ", split based on work performed" : ", split evenly")
                                        + (currencyPayment.PayAsYouGo ? ", paid as work is performed." : ", paid when the project finishes.");
                                }
                                break;
                            }

                        case GrantTitlePayment titlePayment:
                            {
                                desc = $"Receive title `{MessageUtil.StripTags(titlePayment.Title.Name)}` if work contributed is at least *{titlePayment.MinContributedPercent.ToString("F1")}%*.";
                                break;
                            }

                        case KnowledgeSharePayment knowledgePayment:
                            {
                                if (knowledgePayment.Skills.Entries.Count > 0)
                                    desc = $"Receive knowledge of `{MessageUtil.StripTags(knowledgePayment.ShortDescription())}` if work contributed is at least *{knowledgePayment.MinContributedPercent.ToString("F1")}%*.";
                                break;
                            }

                        case ReputationPayment reputationPayment:
                            {
                                float reputationAmountLeft = reputationPayment.Amount - reputationPayment.AmountPaid;
                                desc = $"Receive **{reputationAmountLeft.ToString("F1")} reputation** from *{workParty.Creator.Name}*"
                                    + (reputationPayment.PayType == PayType.SplitByWorkPercent ? ", split based on work performed" : ", split evenly")
                                    + (reputationPayment.PayAsYouGo ? ", paid as work is performed." : ", paid when the project finishes.");
                                break;
                            }

                        default:
                            break;
                    }

                    if (!string.IsNullOrEmpty(desc))
                        paymentDesc += $"- {desc}\n";
                }

                if (!string.IsNullOrWhiteSpace(paymentDesc))
                    embed.AddField("Payment", paymentDesc);

                if (embed.Fields.Count > 0)
                    tagAndContent.Add(new Tuple<string, DiscordLinkEmbed>(tag, new DiscordLinkEmbed(embed)));

                embed.ClearFields();
            }
        }
    }
}
