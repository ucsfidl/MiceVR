function prettify()
% Standard function to make Matlab graphs actually readable and
% viewer-friendly.
%
% Example usage:
% plot(X,Y);
% prettify();

set(gcf, 'color', 'white');  % Set background to white from grey
box off;
set(gca, 'TickDir', 'out');
%set(gca, 'TickLength', [0 0]);  % Get rid of the annoying tick marks on the axes

end