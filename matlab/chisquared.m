function [p chi2stat] = chisquared(class1observed, class1expected, class2observed, class2expected)
% Helper function to do a chisquared test for when I have catch trials available.
% Matlab should have a built-in function for this but it looks like it does not.
% After the 2nd method here: 
% https://www.mathworks.com/matlabcentral/answers/96572-how-can-i-perform-a-chi-square-test-to-determine-how-statistically-different-two-proportions-are-in

% Expected counts under H0 (null hypothesis)
observed = [class1observed class2observed];
expected = [class1expected class2expected];
chi2stat = sum((observed - expected).^2 ./ expected);
p = 1 - chi2cdf(chi2stat, 1);

end