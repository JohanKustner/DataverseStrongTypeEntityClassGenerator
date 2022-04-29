#nullable enable

using System.CodeDom;

namespace CrmDeveloperToolkitExtender.CrmSvcUtil
{
    /// <summary>
    ///     A generate entities.
    /// </summary>
    /// <remarks>
    ///     Johan Küstner, 17/03/2022.
    /// </remarks>
    internal class GenerateEntities
    {
        #region Properties

        /// <summary>
        ///     The entity class.
        /// </summary>
        private CodeTypeDeclaration? _entityClass;

        /// <summary>
        ///     The code constructor.
        /// </summary>
        private CodeConstructor? _codeConstructor;

        /// <summary>
        ///     The comment.
        /// </summary>
        private string? _comment;

        /// <summary>
        ///     (Immutable) the open summary.
        /// </summary>
        private const string OpenSummary = "<Summary>";

        /// <summary>
        ///     (Immutable) the close summary.
        /// </summary>
        private const string CloseSummary = "</Summary>";

        #endregion

        #region Internal Methods

        /// <summary>
        ///     Updates the entity class default constructors comments described by inputClass.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 17/03/2022.
        /// </remarks>
        /// <param name="inputClass">
        ///     The input class.
        /// </param>
        /// <returns>
        ///     A CodeTypeDeclaration.
        /// </returns>
        internal CodeTypeDeclaration? UpdateEntityClassDefaultConstructorsComments(CodeTypeDeclaration? inputClass)
        {
            _entityClass = inputClass;
            if (_entityClass?.Members == null)
            {
                return _entityClass;
            }
            foreach (CodeTypeMember member in _entityClass?.Members!)
            {
                _codeConstructor = (member as CodeConstructor)!;
                UpdateDefaultConstructorComments();
            }
            return _entityClass;
        }

        /// <summary>
        ///     Updates the entity class comments described by inputClass.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 17/03/2022.
        /// </remarks>
        /// <param name="inputClass">
        ///     The input class.
        /// </param>
        /// <returns>
        ///     A CodeTypeDeclaration.
        /// </returns>
        internal CodeTypeDeclaration? UpdateEntityClassComments(CodeTypeDeclaration? inputClass)
        {
            _entityClass = inputClass;
            if (_entityClass?.Members == null)
            {
                return _entityClass;
            }
            foreach (CodeTypeMember member in _entityClass?.Members!)
            {
                if (member.Comments.Count != 0)
                {
                    continue;
                }

                member.Comments.Add(new CodeCommentStatement(OpenSummary, true));
                member.Comments.Add(new CodeCommentStatement(member.Name, true));
                member.Comments.Add(new CodeCommentStatement(CloseSummary, true));
            }

            return _entityClass;
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Updates the default constructor comments.
        /// </summary>
        /// <remarks>
        ///     Johan Küstner, 17/03/2022.
        /// </remarks>
        private void UpdateDefaultConstructorComments()
        {
            if (_codeConstructor == null)
            {
                return;
            }
            _codeConstructor.Comments.Add(new CodeCommentStatement(OpenSummary, true));
            _comment = "Default Constructor.";
            _codeConstructor.Comments.Add(new CodeCommentStatement(_comment, true));
            _codeConstructor.Comments.Add(new CodeCommentStatement(CloseSummary, true));
        }

        #endregion
    }
}
