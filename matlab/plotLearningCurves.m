function plotLearningCurves()
% Takes a Google Spreadsheet, and plots the learning curves.
% Each tab is a mouse, and 2 plots are generated per moues:
% (1) Overall accuracy on the session
% (2) Accuracy per choice on each session
% In both graphs, changes in the level will be marked with a vertical line,
% ideally with the name of the level labeling the line (or available as a
% tooltip on mouseOver). Also, changes in the field restriction will also
% result in a line being drawn, so the viewer is not under the mistaken
% impression that the mouse performed worse because they had forgotten
% something they previously knew.

% My Berkeley sheets are at 1X75Ckw-l5QzzpPPgbrmRsZCOvaNGkEdSk8txUEq53nY
% I enabled anyone can view with link privileges, to get this to work.

% This program uses GetGoogleSpreadsheet, from the File Exchange, to read
% the spreadsheet from the web into Matlab memory.

% Set the docid below to your sheet's unique id in its URL

docid = '1X75Ckw-l5QzzpPPgbrmRsZCOvaNGkEdSk8txUEq53nY';

gids = {'Andor', ...
        'Birdy', ...
        'Crinkle', ...
        'Daria', ...
        };

result = GetGoogleSpreadsheet(docid);



end