function plotEffectOfLesion(loc, mouseName, pre_3c_days, pre_4c_days, post_3c_days, post_4c_days, leftOrRight, analyzeCensored, movingAvg)

% This script produces plots of sight pre- and post-lesion across 3- and 4-choice.  
% These plots will hopefully be the main figures for the paper.

% leftOrRight = 0 for left, 1 for right

pre_3c_sight = zeros(2, length(pre_3c_days));
pre_4c_sight = zeros(2, length(pre_4c_days));
post_3c_sight = zeros(2, length(post_3c_days));
post_4c_Lsight = zeros(2, length(post_4c_days));

for i=1:length(pre_3c_days)
    [pre_3c_sight(1, i), pre_3c_sight(2, i)] = getStats(loc, mouseName, pre_3c_days(i), [], 0, 0, analyzeCensored);
end
for i=1:length(pre_4c_days)
    [pre_4c_sight(1, i), pre_4c_sight(2, i)] = getStats(loc, mouseName, pre_4c_days(i), [], 0, 0, analyzeCensored);
end
for i=1:length(post_3c_days)
    [post_3c_sight(1, i), post_3c_sight(2, i)] = getStats(loc, mouseName, post_3c_days(i), [], 0, 0, analyzeCensored);
end
for i=1:length(post_4c_days)
    [post_4c_sight(1, i), post_4c_sight(2, i)] = getStats(loc, mouseName, post_4c_days(i), [], 0, 0, analyzeCensored);
end

lesionDay = length(pre_3c_days) + 0.5;

idx = leftOrRight+1;

figure
plot(movmean([pre_3c_sight(idx, :) post_3c_sight(idx, :)], [movingAvg movingAvg])*100, '-o', 'MarkerFaceColor', 'b', 'LineWidth', 2);
hold on
plot(movmean([pre_4c_sight(idx, :) post_4c_sight(idx, :)], [movingAvg movingAvg])*100, '-o', 'MarkerFaceColor', 'r', 'LineWidth', 2);

% Cover at least 0-100 in the Y
ymm = ylim;
if (ymm(1) > -9)
    ymm(1) = -9;
end
if (ymm(2) < 109)
    ymm(2) = 109;
end
ylim(ymm);
xmm = xlim;
%xlim([0.1 xmm(2)-0.1]);

plot([lesionDay lesionDay], ylim, 'LineWidth', 3, 'Color', 'k');

xlabel('training day');
ylabel('sight rate (%)');
title(mouseName);
legend('3 choice (right)', '4 choice (right)', 'lesion', 'Location', 'southwest');

prettify();

end