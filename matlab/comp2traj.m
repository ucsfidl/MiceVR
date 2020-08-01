function comp2traj(mouseName, days1, days2, trialTypeStrArr, color1, color2)

[f, mX, mZ, xCI95, zCI95] = analyzeTraj(mouseName, days2, [], [], trialTypeStrArr, 0, 1, 4, 0.0125, 0, 0, 0, 1, color2);
analyzeTraj(mouseName, days1, [], [], trialTypeStrArr, 0, 1, 4, 0.0125, 0, 0, 0, 1, color1);
analyzeTraj(mouseName, days1, [], [], trialTypeStrArr, 0, 1, 4, 0.0125, 0, 0, 0, 0, color1);
plotFilledEllipses(mX, mZ, xCI95(2,:), zCI95(2,:), color2);
title([mouseName ': [' num2str(days1) '] v [' num2str(days2) ']']);

end