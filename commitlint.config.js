module.exports = {
	extends: ["@commitlint/config-conventional", "@commitlint/config-lerna-scopes"],
	rules: {
		"subject-full-stop": [2, "always", "."],
		"header-max-length": [1, "always", 132],
	},
};
