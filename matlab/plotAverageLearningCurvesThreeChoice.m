function plotAverageLearningCurvesThreeChoice
% This function is used to generate a supplemental figure for the first manuscript.
%
% We hard-code a list of mice and days to use for averaging.  Update this list as more mice are done.
%
% Each trial type is shown as a single line, 3 lines for the whole plot, with light shading indicating SEM.
%
% A separate function will be used to plot 4-choice learning curves (plotAverageLearningCurvesFourChoice).
% 
% The actual action files are used for this analysis, not the Google Sheet.
% These are synce by Google Backup to my laptop, so all the data files are available.
% 
% Some mice were censored if they had to be put back on a different task before succeeding on 3-choice
% - Omni
% - Fire

% The data cell array has the mouseName, startDay, and endDay

dataToPlot = {'Plum', 18, 45;
                'Ume', 10, 28;
                'Quark', 23, 25;
                'Grundge', 8, 19;
                'Kalbi', 9, 12;
                'Elf', 7, 22;
                'Cranberry', 17, 22;
                'Ding', 17, 29;
                'Uranus', 28, 31;
                'Fern', 7, 13;
                'Mania', 14, 19;
                'Ink', 8, 13;
                'Hanker', 7, 15;
                'Narc', 9, 13;
                'Sandpiper', 16, 26;
                'Underwood', 12, 15;
                'Rufus', 11, 16;
                'Navel', 12, 22;
                'Velvet', 8, 14;
                'Worm', 15, 18;
                'Taro', 16, 20;
                'Visp', 12, 25;
                'Feather', 7, 11;
                'Justice', 7, 12;
                'Orange', 27, 29;
                'Quill', 27, 29;
                };

   

end