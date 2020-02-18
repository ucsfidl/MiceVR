function f = initTrajFig(figN)

f = figure;
hold on;
ax = gca;
ax.XRuler.Exponent = 0;
ax.YRuler.Exponent = 0;
%axis off;
axis square;
set (gcf, 'Color', 'white')
axis off;
set(f, 'Position', [68+(mod(figN-1,4))*448 634-mod(floor((figN-1)/4),2)*420 448 420])

set(f, 'MenuBar', 'none');
set(f, 'ToolBar', 'none');

end