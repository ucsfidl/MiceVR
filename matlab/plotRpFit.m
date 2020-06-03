function plotRpFit(pupilDiam, Rp)
% after taking 3 or more Rp measurements, run this function which will show
% the fitted line and return the coefficients m and b (for y=mx+b).

[c,S] = polyfit(pupilDiam, Rp, 1);
[y_fit,delta] = polyval(c, pupilDiam, S);

R_sq = 1 - (S.normr/norm(Rp - mean(Rp)))^2;

figure;
plot(pupilDiam, Rp, 'bo')
hold on
plot(pupilDiam, y_fit, 'r-')
plot(sort(pupilDiam), sort(y_fit+2*delta), 'm--', sort(pupilDiam), sort(y_fit-2*delta), 'm--')
title('Linear Fit of Data with 95% Prediction Interval')
legend('Data','Linear Fit','95% Prediction Interval')

m = c(1);
b = c(2);

disp(['m = ' num2str(m)]);
disp(['b = ' num2str(b)]);
disp(['R_squared = ' num2str(R_sq)]);

end