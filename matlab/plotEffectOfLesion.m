function plotEffectOfLesion(loc, mouseName, pre_3c_days, pre_comp_days, post_3c_days, post_comp_days, leftOrRight, analyzeCensored)

% This script produces plots of sight pre- and post-lesion across 3- and 4-choice.  
% These plots will hopefully be the main figures for the paper.

% leftOrRight = 0 for left, 1 for right

movingAvg = 0;

pre_3c_sight = zeros(2, length(pre_3c_days));
pre_comp_sight = zeros(2, length(pre_comp_days));
post_3c_sight = zeros(2, length(post_3c_days));
post_comp_sight = zeros(2, length(post_comp_days));

pre_3c_blindness = zeros(2, length(pre_3c_days));
post_3c_blindness = zeros(2, length(post_3c_days));

for i=1:length(pre_3c_days)
    [pre_3c_sight(1, i), pre_3c_sight(2, i), pre_3c_blindness(1,i), pre_3c_blindness(2,i), ...
        pre_comp_sight(1,i), pre_comp_sight(2,i)] = getStats(loc, mouseName, pre_3c_days(i), [], 0, 0, 0);
end
for i=1:length(pre_comp_days)
    [pre_comp_sight(1, i), pre_comp_sight(2, i)] = getStats(loc, mouseName, pre_comp_days(i), [], 0, 0, analyzeCensored);
end
for i=1:length(post_3c_days)
    [post_3c_sight(1, i), post_3c_sight(2, i), post_3c_blindness(1,i), post_3c_blindness(2, i), ...
        post_comp_sight(1,i), post_comp_sight(2,i)] = getStats(loc, mouseName, post_3c_days(i), [], 0, 0, analyzeCensored);
end
if (~isempty(post_comp_days))
    post_comp_sight = zeros(2, length(post_comp_days));
    for i=1:length(post_comp_days)
        [post_comp_sight(1, i), post_comp_sight(2, i)] = getStats(loc, mouseName, post_comp_days(i), [], 0, 0, analyzeCensored);
    end
end

if (isempty(post_comp_days) && ~isempty(pre_comp_days))
    post_comp_sight = zeros(2, length(post_comp_days));
end

% Move all negative values to 0
pre_3c_sight(1,pre_3c_sight(1,:) < 0) = 0;
pre_3c_sight(2,pre_3c_sight(2,:) < 0) = 0;
post_3c_sight(1,post_3c_sight(1,:) < 0) = 0;
post_3c_sight(2,post_3c_sight(2,:) < 0) = 0;
pre_comp_sight(1,pre_comp_sight(1,:) < 0) = 0;
pre_comp_sight(2,pre_comp_sight(2,:) < 0) = 0;
post_comp_sight(1,post_comp_sight(1,:) < 0) = 0;
post_comp_sight(2,post_comp_sight(2,:) < 0) = 0;
pre_3c_blindness(1,pre_3c_blindness(1,:) < 0) = 0;
pre_3c_blindness(2,pre_3c_blindness(2,:) < 0) = 0;

lesionDay = length(pre_3c_days) + 0.5;

idx = leftOrRight+1;

y3c = [pre_3c_sight(idx,:) post_3c_sight(idx, :)];
ycc = [pre_comp_sight(idx,:) post_comp_sight(idx, :)];
xlen = max(length(y3c), length(ycc));

figure
plot(movmean(y3c, [movingAvg movingAvg])*100, '-o', 'MarkerFaceColor', 'b', 'LineWidth', 2);
hold on
plot(movmean(ycc, [movingAvg movingAvg])*100, '-o', 'MarkerFaceColor', 'r', 'LineWidth', 2);

% Cover [-40 100] in the Y
ylim([0 109])

% Covers 1 before and 1 after the data on the x axis
xmm = xlim;
if (xmm(2) == length(pre_3c_days) + max(length(post_3c_days), length(post_comp_days)))
    xlim([0.1 xmm(2)+0.9]);
else
    xlim([0.1 xmm(2)-0.1]);
end
xmm = xlim;

% Plot lesion line
plot([lesionDay lesionDay], ylim, 'LineWidth', 3, 'Color', 'k');

% Plot dashed line at 0
%plot(xlim, [0 0], '--', 'Color', 'k');

xlabel('training day');
ylabel('sight rate (%)');
title(mouseName);
if (idx == 1)
    if (isempty(pre_comp_days) && isempty(post_comp_days))
        legend('3 choice (left)', '3 choice (left-only)', 'lesion', 'Location', 'southwest');
    else
        legend('3 choice (left)', '4 choice (left)', 'lesion', 'Location', 'southwest');
    end
else
    if (isempty(pre_comp_days) && isempty(post_comp_days))
        legend('3 choice (right)', '3 choice (right-only)', 'lesion', 'Location', 'southwest');
    else
        legend('3 choice (right)', '4 choice (right)', 'lesion', 'Location', 'southwest');
    end
end
xticks(1:xlen);
xticklabels([1:length(pre_3c_days) 1:max(length(post_3c_days), length(post_comp_days))]);

prettify();

[~, Bp] = ttest2(pre_3c_blindness(idx,:), post_3c_blindness(idx,:));
pred1 = 4;
if length(pre_3c_blindness) < 5
    pred1 = size(pre_3c_blindness, 2)-1;
end
pred2 = 4;
if length(post_3c_blindness) < 5
    pred2 = size(post_3c_blindness, 2)-1;
end
disp(['Blindness (pre-3-choice vs post-3-choice blind rate): ' num2str(round(mean(pre_3c_blindness(idx, end-pred1:end)))) '% -> ' ...
    num2str(round(mean(post_3c_blindness(idx, end-pred2:end)))) '% (p=' num2str(Bp) ')']);

[~, Sp] = ttest2(post_3c_sight(idx, :), post_comp_sight(idx, :));
pred1 = 4;
if length(post_3c_sight) < 5
    pred1 = size(post_3c_sight, 2)-1;
end
pred2 = 4;
if length(post_comp_sight) < 5
    pred2 = size(post_comp_sight, 2)-1;
end
disp(['Sight (post-3-choice vs post-comp-choice): ' num2str(round(mean(post_3c_sight(idx, end-pred1:end)) * 100)) '% -> ' ...
    num2str(round(mean(post_comp_sight(idx, end-pred2:end)) * 100)) '% (p=' num2str(Sp) ')']);

end